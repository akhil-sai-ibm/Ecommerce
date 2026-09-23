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
using ECommerce.Infrastructure.Data;
using ECommerce.Domain.Entities;
using Xunit;
using System.Linq;

namespace ECommerce.IntegrationTests
{
    public class InventoryIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private static readonly Guid AdminRoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        public InventoryIntegrationTests(WebApplicationFactory<Program> factory)
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

        private async Task<string> GetCustomerTokenAsync()
        {
            var req = CreateUniqueRegisterRequest();
            await _client.PostAsJsonAsync("/api/auth/register", req);
            var loginResp = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = req.Email, Password = req.Password });
            var data = await loginResp.Content.ReadFromJsonAsync<AuthResponse>();
            return data!.Token;
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

        private async Task<Guid> GetOrCreateCategoryAsync()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();
                var cat = db.Categories.FirstOrDefault();
                if (cat == null)
                {
                    cat = new Category { Id = Guid.NewGuid(), Name = "Integration Test Category", Description = "Test" };
                    db.Categories.Add(cat);
                    await db.SaveChangesAsync();
                }
                return cat.Id;
            }
        }

        private async Task<ProductDto> CreateProductAsAdminAsync(string adminToken)
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            var categoryId = await GetOrCreateCategoryAsync();
            var sku = $"SKU-{Guid.NewGuid():N}";

            var createDto = new CreateProductDto
            {
                Name = "Inventory Test Product",
                Price = 10m,
                SKU = sku,
                CategoryId = categoryId
            };

            var resp = await _client.PostAsJsonAsync("/api/products", createDto);
            resp.EnsureSuccessStatusCode();
            return (await resp.Content.ReadFromJsonAsync<ProductDto>())!;
        }

        [Fact]
        public async Task AdminCanIncreaseAndDecreaseStock_ReturnsNoContent()
        {
            var token = await GetAdminTokenAsync();
            var product = await CreateProductAsAdminAsync(token);

            // Increase
            var incResp = await _client.PostAsync($"/api/inventory/{product.Id}/increase?amount=10", null);
            Assert.Equal(HttpStatusCode.NoContent, incResp.StatusCode);

            // Verify quantity
            var getResp1 = await _client.GetAsync($"/api/inventory/{product.Id}");
            var inv1 = await getResp1.Content.ReadFromJsonAsync<InventoryDto>();
            Assert.Equal(10, inv1!.Quantity);

            // Decrease
            var decResp = await _client.PostAsync($"/api/inventory/{product.Id}/decrease?amount=4", null);
            Assert.Equal(HttpStatusCode.NoContent, decResp.StatusCode);

            // Verify quantity
            var getResp2 = await _client.GetAsync($"/api/inventory/{product.Id}");
            var inv2 = await getResp2.Content.ReadFromJsonAsync<InventoryDto>();
            Assert.Equal(6, inv2!.Quantity);
        }

        [Fact]
        public async Task DecreaseStock_BelowZero_ReturnsBadRequest()
        {
            var token = await GetAdminTokenAsync();
            var product = await CreateProductAsAdminAsync(token);

            // Initial quantity is 0, decreasing should fail
            var decResp = await _client.PostAsync($"/api/inventory/{product.Id}/decrease?amount=5", null);
            Assert.Equal(HttpStatusCode.BadRequest, decResp.StatusCode);
        }

        [Fact]
        public async Task GetLowStock_ReturnsCorrectItems()
        {
            var token = await GetAdminTokenAsync();
            var product = await CreateProductAsAdminAsync(token);

            // Set ReorderLevel to 10, and quantity is 0 (which is <= 10)
            var updateDto = new UpdateInventoryDto { ReorderLevel = 10 };
            var updateResp = await _client.PutAsJsonAsync($"/api/inventory/{product.Id}", updateDto);
            updateResp.EnsureSuccessStatusCode();

            // Fetch low stock
            var lowStockResp = await _client.GetAsync("/api/inventory/low");
            Assert.Equal(HttpStatusCode.OK, lowStockResp.StatusCode);

            var lowStockItems = await lowStockResp.Content.ReadFromJsonAsync<List<InventoryDto>>();
            Assert.NotNull(lowStockItems);
            
            // Check if our product is in the low stock list
            Assert.Contains(lowStockItems, i => i.ProductId == product.Id);
        }

        [Fact]
        public async Task CustomerCannotModifyInventory_ReturnsForbidden()
        {
            var adminToken = await GetAdminTokenAsync();
            var product = await CreateProductAsAdminAsync(adminToken);

            var customerToken = await GetCustomerTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);

            var incResp = await _client.PostAsync($"/api/inventory/{product.Id}/increase?amount=10", null);
            Assert.Equal(HttpStatusCode.Forbidden, incResp.StatusCode);
        }

        [Fact]
        public async Task CustomerCannotAccessLowStockEndpoint_ReturnsForbidden()
        {
            var customerToken = await GetCustomerTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);

            var lowStockResp = await _client.GetAsync("/api/inventory/low");
            Assert.Equal(HttpStatusCode.Forbidden, lowStockResp.StatusCode);
        }
    }
}
