using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ECommerce.API;
using ECommerce.Application.DTOs.Auth;
using ECommerce.Application.DTOs.Products;
using ECommerce.Application.DTOs.Inventory;
using ECommerce.Application.DTOs.Categories;
using ECommerce.Application.DTOs.Cart;
using ECommerce.Application.DTOs.Orders;
using ECommerce.Application.DTOs.Payments;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Data;
using Xunit;
using System.Linq;

namespace ECommerce.IntegrationTests
{
    public class CartCheckoutIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private static readonly Guid AdminRoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        public CartCheckoutIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        private RegisterRequest CreateUniqueRegisterRequest()
        {
            var uniqueSuffix = Guid.NewGuid().ToString("N");
            return new RegisterRequest
            {
                FirstName = "Test",
                LastName = "User",
                Email = $"testuser_{uniqueSuffix}@example.com",
                Password = "Password123!"
            };
        }

        private async Task<(string Token, Guid UserId)> GetCustomerTokenAndIdAsync()
        {
            var req = CreateUniqueRegisterRequest();
            await _client.PostAsJsonAsync("/api/auth/register", req);
            var loginResp = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = req.Email, Password = req.Password });
            var data = await loginResp.Content.ReadFromJsonAsync<AuthResponse>();
            return (data!.Token, data.User.Id);
        }

        private async Task<string> GetAdminTokenAsync()
        {
            var req = CreateUniqueRegisterRequest();
            var regResp = await _client.PostAsJsonAsync("/api/auth/register", req);
            var regData = await regResp.Content.ReadFromJsonAsync<AuthResponse>();

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();
                var user = await db.Users.FindAsync(regData!.User.Id);
                user!.RoleId = AdminRoleId;
                await db.SaveChangesAsync();
            }

            var loginResp = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = req.Email, Password = req.Password });
            var data = await loginResp.Content.ReadFromJsonAsync<AuthResponse>();
            return data!.Token;
        }

        private async Task<Guid> GetOrCreateCategoryAsync(string adminToken)
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            var resp = await _client.GetAsync("/api/categories");
            var categories = await resp.Content.ReadFromJsonAsync<List<CategoryDto>>();
            if (categories != null && categories.Count > 0)
                return categories[0].Id;

            var createDto = new CreateCategoryDto { Name = "Test Category", Description = "Test" };
            var createResp = await _client.PostAsJsonAsync("/api/categories", createDto);
            var newCategory = await createResp.Content.ReadFromJsonAsync<CategoryDto>();
            return newCategory!.Id;
        }

        private async Task<ProductDto> CreateProductWithInventoryAsync(string adminToken, decimal price = 100m, int stock = 10)
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            var categoryId = await GetOrCreateCategoryAsync(adminToken);
            var createDto = new CreateProductDto { Name = $"Product {Guid.NewGuid():N}", Price = price, SKU = $"SKU-{Guid.NewGuid():N}", CategoryId = categoryId };
            
            var createResp = await _client.PostAsJsonAsync("/api/products", createDto);
            var product = await createResp.Content.ReadFromJsonAsync<ProductDto>();

            // Update inventory
            await _client.PostAsync($"/api/inventory/{product.Id}/increase?amount={stock}", null);

            return product;
        }

        private CheckoutRequestDto CreateValidCheckoutRequest() => new CheckoutRequestDto
        {
            ShippingAddressLine1 = "123 Main St",
            ShippingCity = "Seattle",
            ShippingState = "WA",
            ShippingPostalCode = "98101",
            ShippingCountry = "USA",
            ShippingPhoneNumber = "555-0199"
        };

        // ==================== CART TESTS ====================

        [Fact]
        public async Task Cart_AddItems_Succeeds()
        {
            var adminToken = await GetAdminTokenAsync();
            var product = await CreateProductWithInventoryAsync(adminToken);

            var (token, _) = await GetCustomerTokenAndIdAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            _client.DefaultRequestHeaders.Remove("X-Idempotency-Key");

            var addResp = await _client.PostAsJsonAsync("/api/cart/items", new AddCartItemDto { ProductId = product.Id, Quantity = 2 });
            Assert.Equal(HttpStatusCode.NoContent, addResp.StatusCode);

            var getResp = await _client.GetAsync("/api/cart");
            var cart = await getResp.Content.ReadFromJsonAsync<CartDto>();
            
            Assert.NotNull(cart);
            Assert.Single(cart.Items);
            Assert.Equal(product.Id, cart.Items[0].ProductId);
            Assert.Equal(2, cart.Items[0].Quantity);
        }

        [Fact]
        public async Task Cart_GetForCurrentUser_ReturnsOnlyMyCart()
        {
            var adminToken = await GetAdminTokenAsync();
            var product = await CreateProductWithInventoryAsync(adminToken);

            // User 1 adds item
            var (token1, _) = await GetCustomerTokenAndIdAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token1);
            await _client.PostAsJsonAsync("/api/cart/items", new AddCartItemDto { ProductId = product.Id, Quantity = 2 });

            // User 2 adds item
            var (token2, _) = await GetCustomerTokenAndIdAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token2);
            await _client.PostAsJsonAsync("/api/cart/items", new AddCartItemDto { ProductId = product.Id, Quantity = 5 });

            // Verify User 1 cart
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token1);
            var getResp1 = await _client.GetAsync("/api/cart");
            var cart1 = await getResp1.Content.ReadFromJsonAsync<CartDto>();
            Assert.Equal(2, cart1!.Items[0].Quantity);

            // Verify User 2 cart
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token2);
            var getResp2 = await _client.GetAsync("/api/cart");
            var cart2 = await getResp2.Content.ReadFromJsonAsync<CartDto>();
            Assert.Equal(5, cart2!.Items[0].Quantity);
        }

        // ==================== CHECKOUT TESTS ====================

        [Fact]
        public async Task Checkout_Success_CreatesOrder_ClearsCart_DeductsInventory()
        {
            var adminToken = await GetAdminTokenAsync();
            var product = await CreateProductWithInventoryAsync(adminToken, 50m, 10);

            var (token, _) = await GetCustomerTokenAndIdAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            await _client.PostAsJsonAsync("/api/cart/items", new AddCartItemDto { ProductId = product.Id, Quantity = 2 });

            // Checkout
            _client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());
            var checkoutResp = await _client.PostAsJsonAsync("/api/orders/checkout", CreateValidCheckoutRequest());
            Assert.Equal(HttpStatusCode.Created, checkoutResp.StatusCode);
            var order = await checkoutResp.Content.ReadFromJsonAsync<OrderDto>();

            // Verify Order
            Assert.NotNull(order);
            Assert.Equal(100m, order.TotalAmount); // 50 * 2
            Assert.Single(order.Items);
            Assert.Equal(50m, order.Items[0].UnitPrice);
            Assert.Equal(2, order.Items[0].Quantity);

            // Verify Cart Cleared
            var getCartResp = await _client.GetAsync("/api/cart");
            var cart = await getCartResp.Content.ReadFromJsonAsync<CartDto>();
            Assert.Empty(cart!.Items);

            // Verify Inventory Deducted (Admin)
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            var invResp = await _client.GetAsync($"/api/inventory/{product.Id}");
            var inv = await invResp.Content.ReadFromJsonAsync<InventoryDto>();
            Assert.Equal(8, inv!.Quantity); // 10 - 2
        }

        [Fact]
        public async Task Checkout_InsufficientInventory_IsRejected()
        {
            var adminToken = await GetAdminTokenAsync();
            var product = await CreateProductWithInventoryAsync(adminToken, 50m, 5); // 5 in stock

            var (token, _) = await GetCustomerTokenAndIdAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
            // Try to add 3 items (this will succeed)
            await _client.PostAsJsonAsync("/api/cart/items", new AddCartItemDto { ProductId = product.Id, Quantity = 3 });

            // Now admin reduces stock to 2
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            await _client.PostAsync($"/api/inventory/{product.Id}/decrease?amount=3", null);

            // Checkout should fail
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            _client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());
            var checkoutResp = await _client.PostAsJsonAsync("/api/orders/checkout", CreateValidCheckoutRequest());
            Assert.Equal(HttpStatusCode.BadRequest, checkoutResp.StatusCode); // DomainException returns 400

            // Verify Cart NOT cleared
            var getCartResp = await _client.GetAsync("/api/cart");
            var cart = await getCartResp.Content.ReadFromJsonAsync<CartDto>();
            Assert.Single(cart!.Items);

            // Verify Inventory NOT deducted
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            var invResp = await _client.GetAsync($"/api/inventory/{product.Id}");
            var inv = await invResp.Content.ReadFromJsonAsync<InventoryDto>();
            Assert.Equal(2, inv!.Quantity);
        }

        // ==================== PAYMENT TESTS ====================

        [Fact]
        public async Task Payment_Success_UpdatesStatusToSuccessful()
        {
            var adminToken = await GetAdminTokenAsync();
            var product = await CreateProductWithInventoryAsync(adminToken, 100m, 5);

            var (token, _) = await GetCustomerTokenAndIdAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            await _client.PostAsJsonAsync("/api/cart/items", new AddCartItemDto { ProductId = product.Id, Quantity = 1 });

            // Checkout
            _client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());
            var checkoutResp = await _client.PostAsJsonAsync("/api/orders/checkout", CreateValidCheckoutRequest());
            var order = await checkoutResp.Content.ReadFromJsonAsync<OrderDto>();

            // Create Payment
            _client.DefaultRequestHeaders.Remove("Idempotency-Key");
            var createPaymentReq = new CreatePaymentDto { OrderId = order!.Id, Amount = order.TotalAmount, Method = PaymentMethod.CreditCard };
            var createPaymentResp = await _client.PostAsJsonAsync("/api/payments", createPaymentReq);
            Assert.Equal(HttpStatusCode.Created, createPaymentResp.StatusCode);
            var payment = await createPaymentResp.Content.ReadFromJsonAsync<PaymentDto>();
            
            Assert.Equal(PaymentStatus.Pending, payment!.Status);

            // Process Payment
            var processResp = await _client.PostAsync($"/api/payments/{payment.Id}/process", null);
            Assert.Equal(HttpStatusCode.OK, processResp.StatusCode);
            
            var processedPayment = await processResp.Content.ReadFromJsonAsync<PaymentDto>();
            Assert.Equal(PaymentStatus.Successful, processedPayment!.Status);
        }

        [Fact]
        public async Task Payment_Failure_UpdatesStatusToFailed()
        {
            var adminToken = await GetAdminTokenAsync();
            // Create product with price > 1,000,000 to trigger payment failure
            var product = await CreateProductWithInventoryAsync(adminToken, 1500000m, 5);

            var (token, _) = await GetCustomerTokenAndIdAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            await _client.PostAsJsonAsync("/api/cart/items", new AddCartItemDto { ProductId = product.Id, Quantity = 1 });

            // Checkout
            _client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());
            var checkoutResp = await _client.PostAsJsonAsync("/api/orders/checkout", CreateValidCheckoutRequest());
            var order = await checkoutResp.Content.ReadFromJsonAsync<OrderDto>();

            // Create Payment
            _client.DefaultRequestHeaders.Remove("Idempotency-Key");
            var createPaymentReq = new CreatePaymentDto { OrderId = order!.Id, Amount = order.TotalAmount, Method = PaymentMethod.CreditCard };
            var createPaymentResp = await _client.PostAsJsonAsync("/api/payments", createPaymentReq);
            var payment = await createPaymentResp.Content.ReadFromJsonAsync<PaymentDto>();

            // Process Payment - Should fail
            var processResp = await _client.PostAsync($"/api/payments/{payment!.Id}/process", null);
            Assert.Equal(HttpStatusCode.BadRequest, processResp.StatusCode); // Fails via DomainException -> 400 Bad Request

            // Verify Payment Status is Failed
            var getPaymentResp = await _client.GetAsync($"/api/payments/{payment.Id}");
            var updatedPayment = await getPaymentResp.Content.ReadFromJsonAsync<PaymentDto>();
            Assert.Equal(PaymentStatus.Failed, updatedPayment!.Status);

            // Verify Order remains created/unchanged (likely Pending)
            var getOrderResp = await _client.GetAsync($"/api/orders/{order.Id}");
            var unchangedOrder = await getOrderResp.Content.ReadFromJsonAsync<OrderDto>();
            Assert.Equal(OrderStatus.Pending, unchangedOrder!.Status); // Order still pending despite payment failure

            // Verify Cart remains empty
            var getCartResp = await _client.GetAsync("/api/cart");
            var cart = await getCartResp.Content.ReadFromJsonAsync<CartDto>();
            Assert.Empty(cart!.Items);

            // Verify Inventory remains deducted
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            var invResp = await _client.GetAsync($"/api/inventory/{product.Id}");
            var inv = await invResp.Content.ReadFromJsonAsync<InventoryDto>();
            Assert.Equal(4, inv!.Quantity); // 5 - 1
        }

        [Fact]
        public async Task Payment_CustomerCannotPayForSomeoneElsesOrder()
        {
            var adminToken = await GetAdminTokenAsync();
            var product = await CreateProductWithInventoryAsync(adminToken, 100m, 5);

            // User 1 Creates Order
            var (token1, _) = await GetCustomerTokenAndIdAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token1);
            await _client.PostAsJsonAsync("/api/cart/items", new AddCartItemDto { ProductId = product.Id, Quantity = 1 });
            var checkoutResp = await _client.PostAsJsonAsync("/api/orders/checkout", CreateValidCheckoutRequest());
            var order = await checkoutResp.Content.ReadFromJsonAsync<OrderDto>();

            // User 2 Attempts to Pay
            var (token2, _) = await GetCustomerTokenAndIdAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token2);
            
            var createPaymentReq = new CreatePaymentDto { OrderId = order!.Id, Amount = order.TotalAmount, Method = PaymentMethod.CreditCard };
            var createPaymentResp = await _client.PostAsJsonAsync("/api/payments", createPaymentReq);
            
            Assert.Equal(HttpStatusCode.Forbidden, createPaymentResp.StatusCode);
        }

        // ==================== IDEMPOTENCY TESTS ====================

        [Fact]
        public async Task Idempotency_RepeatRequest_DoesNotCreateDuplicateOrder()
        {
            var adminToken = await GetAdminTokenAsync();
            var product = await CreateProductWithInventoryAsync(adminToken, 100m, 10);

            var (token, _) = await GetCustomerTokenAndIdAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            await _client.PostAsJsonAsync("/api/cart/items", new AddCartItemDto { ProductId = product.Id, Quantity = 1 });

            // Checkout with Idempotency Key
            var idempotencyKey = Guid.NewGuid().ToString();
            _client.DefaultRequestHeaders.Add("Idempotency-Key", idempotencyKey);
            
            var checkoutResp1 = await _client.PostAsJsonAsync("/api/orders/checkout", CreateValidCheckoutRequest());
            Assert.Equal(HttpStatusCode.Created, checkoutResp1.StatusCode);
            var order1 = await checkoutResp1.Content.ReadFromJsonAsync<OrderDto>();

            // Repeat request with SAME Idempotency Key
            var checkoutResp2 = await _client.PostAsJsonAsync("/api/orders/checkout", CreateValidCheckoutRequest());
            Assert.Equal(HttpStatusCode.Created, checkoutResp2.StatusCode); 
            var order2 = await checkoutResp2.Content.ReadFromJsonAsync<OrderDto>();
            Assert.Equal(order1!.Id, order2!.Id);

            // Verify only one order was created for this user
            var getOrdersResp = await _client.GetAsync("/api/orders/me");
            var orders = await getOrdersResp.Content.ReadFromJsonAsync<List<OrderDto>>();
            Assert.Single(orders!);
            Assert.Equal(order1!.Id, orders![0].Id);

            // Verify inventory deducted only once (10 - 1 = 9)
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            _client.DefaultRequestHeaders.Remove("Idempotency-Key");
            var invResp = await _client.GetAsync($"/api/inventory/{product.Id}");
            var inv = await invResp.Content.ReadFromJsonAsync<InventoryDto>();
            Assert.Equal(9, inv!.Quantity);
        }

        [Fact]
        public async Task Idempotency_DifferentKeysAreIndependent()
        {
            var adminToken = await GetAdminTokenAsync();
            var product = await CreateProductWithInventoryAsync(adminToken, 100m, 10);

            var (token, _) = await GetCustomerTokenAndIdAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
            // Checkout 1
            await _client.PostAsJsonAsync("/api/cart/items", new AddCartItemDto { ProductId = product.Id, Quantity = 1 });
            _client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());
            var checkoutResp1 = await _client.PostAsJsonAsync("/api/orders/checkout", CreateValidCheckoutRequest());
            Assert.Equal(HttpStatusCode.Created, checkoutResp1.StatusCode);
            _client.DefaultRequestHeaders.Remove("Idempotency-Key"); // Remove for next steps

            // Checkout 2
            await _client.PostAsJsonAsync("/api/cart/items", new AddCartItemDto { ProductId = product.Id, Quantity = 1 });
            _client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());
            var checkoutResp2 = await _client.PostAsJsonAsync("/api/orders/checkout", CreateValidCheckoutRequest());
            Assert.Equal(HttpStatusCode.Created, checkoutResp2.StatusCode);
            _client.DefaultRequestHeaders.Remove("Idempotency-Key");

            // Verify TWO orders were created
            var getOrdersResp = await _client.GetAsync("/api/orders/me");
            var orders = await getOrdersResp.Content.ReadFromJsonAsync<List<OrderDto>>();
            Assert.Equal(2, orders!.Count);

            // Verify inventory deducted twice (10 - 2 = 8)
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            var invResp = await _client.GetAsync($"/api/inventory/{product.Id}");
            var inv = await invResp.Content.ReadFromJsonAsync<InventoryDto>();
            Assert.Equal(8, inv!.Quantity);
        }

        // ==================== ORDER OWNERSHIP TESTS ====================

        [Fact]
        public async Task Order_CustomerCannotViewAnotherCustomersOrder()
        {
            var adminToken = await GetAdminTokenAsync();
            var product = await CreateProductWithInventoryAsync(adminToken, 100m, 5);

            // User 1 Creates Order
            var (token1, _) = await GetCustomerTokenAndIdAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token1);
            await _client.PostAsJsonAsync("/api/cart/items", new AddCartItemDto { ProductId = product.Id, Quantity = 1 });
            var checkoutResp = await _client.PostAsJsonAsync("/api/orders/checkout", CreateValidCheckoutRequest());
            var order = await checkoutResp.Content.ReadFromJsonAsync<OrderDto>();

            // User 2 Attempts to View
            var (token2, _) = await GetCustomerTokenAndIdAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token2);
            
            var getOrderResp = await _client.GetAsync($"/api/orders/{order!.Id}");
            Assert.Equal(HttpStatusCode.Forbidden, getOrderResp.StatusCode);
        }
    }
}
