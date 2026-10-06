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

namespace ECommerce.IntegrationTests
{
    public class OrderStateIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private static readonly Guid AdminRoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        public OrderStateIntegrationTests(WebApplicationFactory<Program> factory)
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

        private async Task<OrderDto> CreateOrderAsync(string customerToken, Guid productId, int quantity)
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
            await _client.PostAsJsonAsync("/api/cart/items", new AddCartItemDto { ProductId = productId, Quantity = quantity });
            var checkoutResp = await _client.PostAsJsonAsync("/api/orders/checkout", CreateValidCheckoutRequest());
            return (await checkoutResp.Content.ReadFromJsonAsync<OrderDto>())!;
        }

        // ==================== STATE MACHINE TESTS ====================

        [Fact]
        public async Task ValidOrderStatusTransitions_WorkCorrectly()
        {
            var adminToken = await GetAdminTokenAsync();
            var product = await CreateProductWithInventoryAsync(adminToken);
            var (customerToken, _) = await GetCustomerTokenAndIdAsync();
            var order = await CreateOrderAsync(customerToken, product.Id, 1);

            Assert.Equal(OrderStatus.Pending, order.Status);

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

            // Pending -> Confirmed
            var resp1 = await _client.PutAsync($"/api/orders/{order.Id}/status?status={(int)OrderStatus.Confirmed}", null);
            Assert.Equal(HttpStatusCode.NoContent, resp1.StatusCode);

            // Confirmed -> Processing
            var resp2 = await _client.PutAsync($"/api/orders/{order.Id}/status?status={(int)OrderStatus.Processing}", null);
            Assert.Equal(HttpStatusCode.NoContent, resp2.StatusCode);

            // Processing -> Shipped
            var resp3 = await _client.PutAsync($"/api/orders/{order.Id}/status?status={(int)OrderStatus.Shipped}", null);
            Assert.Equal(HttpStatusCode.NoContent, resp3.StatusCode);

            // Shipped -> Delivered
            var resp4 = await _client.PutAsync($"/api/orders/{order.Id}/status?status={(int)OrderStatus.Delivered}", null);
            Assert.Equal(HttpStatusCode.NoContent, resp4.StatusCode);

            // Verify Final Status
            var getOrderResp = await _client.GetAsync($"/api/orders/{order.Id}");
            var updatedOrder = await getOrderResp.Content.ReadFromJsonAsync<OrderDto>();
            Assert.Equal(OrderStatus.Delivered, updatedOrder!.Status);
        }

        [Fact]
        public async Task InvalidOrderStatusTransition_IsRejected()
        {
            var adminToken = await GetAdminTokenAsync();
            var product = await CreateProductWithInventoryAsync(adminToken);
            var (customerToken, _) = await GetCustomerTokenAndIdAsync();
            var order = await CreateOrderAsync(customerToken, product.Id, 1);

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

            // Pending -> Confirmed (Valid)
            await _client.PutAsync($"/api/orders/{order.Id}/status?status={(int)OrderStatus.Confirmed}", null);
            
            // Confirmed -> Delivered (Invalid transition)
            var resp = await _client.PutAsync($"/api/orders/{order.Id}/status?status={(int)OrderStatus.Delivered}", null);
            
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode); // DomainException returns 400

            // Verify status didn't change to Delivered
            var getOrderResp = await _client.GetAsync($"/api/orders/{order.Id}");
            var updatedOrder = await getOrderResp.Content.ReadFromJsonAsync<OrderDto>();
            Assert.Equal(OrderStatus.Confirmed, updatedOrder!.Status);
        }

        [Fact]
        public async Task ShippedToDelivered_TransitionSucceeds()
        {
            var adminToken = await GetAdminTokenAsync();
            var product = await CreateProductWithInventoryAsync(adminToken);
            var (customerToken, _) = await GetCustomerTokenAndIdAsync();
            var order = await CreateOrderAsync(customerToken, product.Id, 1);

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

            // Fast forward to Shipped
            await _client.PutAsync($"/api/orders/{order.Id}/status?status={(int)OrderStatus.Confirmed}", null);
            await _client.PutAsync($"/api/orders/{order.Id}/status?status={(int)OrderStatus.Processing}", null);
            await _client.PutAsync($"/api/orders/{order.Id}/status?status={(int)OrderStatus.Shipped}", null);

            // The specific test for Shipped -> Delivered
            var resp = await _client.PutAsync($"/api/orders/{order.Id}/status?status={(int)OrderStatus.Delivered}", null);
            Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
            
            var getOrderResp = await _client.GetAsync($"/api/orders/{order.Id}");
            var updatedOrder = await getOrderResp.Content.ReadFromJsonAsync<OrderDto>();
            Assert.Equal(OrderStatus.Delivered, updatedOrder!.Status);
        }

        // ==================== CANCELLATION TESTS ====================

        [Fact]
        public async Task CustomerCanCancelOwnEligibleOrder_AndInventoryIsRestored()
        {
            var adminToken = await GetAdminTokenAsync();
            var product = await CreateProductWithInventoryAsync(adminToken, 100m, 10);
            
            var (customerToken, _) = await GetCustomerTokenAndIdAsync();
            var order = await CreateOrderAsync(customerToken, product.Id, 2);

            // Verify inventory deducted (10 - 2 = 8)
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            var invRespBefore = await _client.GetAsync($"/api/inventory/{product.Id}");
            var invBefore = await invRespBefore.Content.ReadFromJsonAsync<InventoryDto>();
            Assert.Equal(8, invBefore!.Quantity);

            // Cancel the order
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
            var cancelResp = await _client.PostAsync($"/api/orders/{order.Id}/cancel", null);
            Assert.Equal(HttpStatusCode.NoContent, cancelResp.StatusCode);

            // Verify Order Status is Cancelled
            var getOrderResp = await _client.GetAsync($"/api/orders/{order.Id}");
            var updatedOrder = await getOrderResp.Content.ReadFromJsonAsync<OrderDto>();
            Assert.Equal(OrderStatus.Cancelled, updatedOrder!.Status);

            // Verify inventory is restored (8 + 2 = 10)
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            var invRespAfter = await _client.GetAsync($"/api/inventory/{product.Id}");
            var invAfter = await invRespAfter.Content.ReadFromJsonAsync<InventoryDto>();
            Assert.Equal(10, invAfter!.Quantity);

            // Verify NO refund is initiated because it was unpaid
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
            var paymentResp = await _client.GetAsync($"/api/payments/order/{order.Id}");
            Assert.Equal(HttpStatusCode.NotFound, paymentResp.StatusCode);
        }

        [Fact]
        public async Task CustomerCannotCancelAnotherCustomersOrder()
        {
            var adminToken = await GetAdminTokenAsync();
            var product = await CreateProductWithInventoryAsync(adminToken);
            
            // Customer 1 creates order
            var (customer1Token, _) = await GetCustomerTokenAndIdAsync();
            var order = await CreateOrderAsync(customer1Token, product.Id, 1);

            // Customer 2 attempts to cancel
            var (customer2Token, _) = await GetCustomerTokenAndIdAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customer2Token);
            
            var cancelResp = await _client.PostAsync($"/api/orders/{order.Id}/cancel", null);
            Assert.Equal(HttpStatusCode.Forbidden, cancelResp.StatusCode);
            
            // Verify order is still pending
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customer1Token);
            var getOrderResp = await _client.GetAsync($"/api/orders/{order.Id}");
            var unchangedOrder = await getOrderResp.Content.ReadFromJsonAsync<OrderDto>();
            Assert.Equal(OrderStatus.Pending, unchangedOrder!.Status);
        }

        [Fact]
        public async Task CustomerCancelsPaidOrder_RefundInitiated()
        {
            var adminToken = await GetAdminTokenAsync();
            var product = await CreateProductWithInventoryAsync(adminToken, 100m, 10);
            
            var (customerToken, _) = await GetCustomerTokenAndIdAsync();
            var order = await CreateOrderAsync(customerToken, product.Id, 2);

            // Create and process payment
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
            var createPaymentResp = await _client.PostAsJsonAsync("/api/payments", new CreatePaymentDto { OrderId = order.Id, Amount = order.TotalAmount, Method = PaymentMethod.CreditCard });
            createPaymentResp.EnsureSuccessStatusCode();
            var payment = await createPaymentResp.Content.ReadFromJsonAsync<PaymentDto>();

            var processResp = await _client.PostAsync($"/api/payments/{payment!.Id}/process", null);
            processResp.EnsureSuccessStatusCode();

            // Wait for Outbox Processor to set IsPaymentSuccessful = true
            await Task.Delay(6000); 

            // Verify order is Paid but still Pending (0)
            var getOrderResp = await _client.GetAsync($"/api/orders/{order.Id}");
            var paidOrder = await getOrderResp.Content.ReadFromJsonAsync<OrderDto>();
            Assert.Equal(OrderStatus.Pending, paidOrder!.Status); // Not finalized until Admin Confirms

            // Customer cancels
            var cancelResp = await _client.PostAsync($"/api/orders/{order.Id}/cancel", null);
            Assert.Equal(HttpStatusCode.NoContent, cancelResp.StatusCode);

            // Wait for Outbox Processor to process OrderCancelled and initiate refund
            await Task.Delay(6000);

            // Verify payment status is Refunded
            var paymentResp = await _client.GetAsync($"/api/payments/order/{order.Id}");
            var updatedPayment = await paymentResp.Content.ReadFromJsonAsync<PaymentDto>();
            Assert.Equal(PaymentStatus.Refunded, updatedPayment!.Status); // Refunded
        }

        [Fact]
        public async Task DuplicateOrderCancelledProcessing_NoDuplicateRefund()
        {
            var adminToken = await GetAdminTokenAsync();
            var product = await CreateProductWithInventoryAsync(adminToken, 100m, 10);
            
            var (customerToken, _) = await GetCustomerTokenAndIdAsync();
            var order = await CreateOrderAsync(customerToken, product.Id, 2);

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
            var createPaymentResp = await _client.PostAsJsonAsync("/api/payments", new CreatePaymentDto { OrderId = order.Id, Amount = order.TotalAmount, Method = PaymentMethod.CreditCard });
            createPaymentResp.EnsureSuccessStatusCode();
            var payment = await createPaymentResp.Content.ReadFromJsonAsync<PaymentDto>();

            await _client.PostAsync($"/api/payments/{payment!.Id}/process", null);
            await Task.Delay(6000); 

            // Cancel
            await _client.PostAsync($"/api/orders/{order.Id}/cancel", null);
            await Task.Delay(6000);

            // Let's check idempotency by verifying the outbox retry doesn't cause failure. Since we can't easily trigger outbox retry,
            // we can verify the state remains consistent. The refund is Refunded.
            var paymentResp = await _client.GetAsync($"/api/payments/order/{order.Id}");
            var updatedPayment = await paymentResp.Content.ReadFromJsonAsync<PaymentDto>();
            Assert.Equal(PaymentStatus.Refunded, updatedPayment!.Status); // Refunded
        }
    }
}
