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
    public class ProductIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private static readonly Guid AdminRoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        public ProductIntegrationTests(WebApplicationFactory<Program> factory)
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

        [Fact]
        public async Task GetProducts_ReturnsOk()
        {
            var response = await _client.GetAsync("/api/products");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>();
            Assert.NotNull(products);
        }

        [Fact]
        public async Task GetProductById_NonExisting_ReturnsNotFound()
        {
            var response = await _client.GetAsync($"/api/products/{Guid.NewGuid()}");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task AdminCanCreateProduct_And_GetProductById_ReturnsOk()
        {
            var token = await GetAdminTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var categoryId = await GetOrCreateCategoryAsync();
            var sku = $"SKU-{Guid.NewGuid():N}";

            var createDto = new CreateProductDto
            {
                Name = "Test Product",
                Description = "Test Desc",
                Price = 99.99m,
                SKU = sku,
                CategoryId = categoryId
            };

            // Create Product
            var createResp = await _client.PostAsJsonAsync("/api/products", createDto);
            Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
            
            var createdProduct = await createResp.Content.ReadFromJsonAsync<ProductDto>();
            Assert.NotNull(createdProduct);
            Assert.Equal(sku, createdProduct.SKU);

            // Get Product By Id
            var getResp = await _client.GetAsync($"/api/products/{createdProduct.Id}");
            Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
            var fetchedProduct = await getResp.Content.ReadFromJsonAsync<ProductDto>();
            Assert.NotNull(fetchedProduct);
            Assert.Equal(createdProduct.Id, fetchedProduct.Id);
            
            // Verify Inventory was automatically created
            var invResp = await _client.GetAsync($"/api/inventory/{createdProduct.Id}");
            Assert.Equal(HttpStatusCode.OK, invResp.StatusCode);
            var invData = await invResp.Content.ReadFromJsonAsync<InventoryDto>();
            Assert.NotNull(invData);
            Assert.Equal(0, invData.Quantity);
        }

        [Fact]
        public async Task CustomerCannotCreateProduct_ReturnsForbidden()
        {
            var token = await GetCustomerTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var createDto = new CreateProductDto { Name = "Customer Product", Price = 10, SKU = "CUST-1", CategoryId = Guid.NewGuid() };
            var response = await _client.PostAsJsonAsync("/api/products", createDto);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task AdminCanUpdateProduct_ReturnsNoContent()
        {
            var token = await GetAdminTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var categoryId = await GetOrCreateCategoryAsync();
            var createDto = new CreateProductDto { Name = "Old Name", Price = 10, SKU = $"SKU-{Guid.NewGuid():N}", CategoryId = categoryId };
            var createResp = await _client.PostAsJsonAsync("/api/products", createDto);
            var createdProduct = await createResp.Content.ReadFromJsonAsync<ProductDto>();

            var updateDto = new UpdateProductDto { Name = "New Name" };
            var updateResp = await _client.PutAsJsonAsync($"/api/products/{createdProduct!.Id}", updateDto);
            
            Assert.Equal(HttpStatusCode.NoContent, updateResp.StatusCode);

            var getResp = await _client.GetAsync($"/api/products/{createdProduct.Id}");
            var fetchedProduct = await getResp.Content.ReadFromJsonAsync<ProductDto>();
            Assert.Equal("New Name", fetchedProduct!.Name);
        }

        [Fact]
        public async Task AdminCanDeleteProduct_ReturnsOk()
        {
            var token = await GetAdminTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var categoryId = await GetOrCreateCategoryAsync();
            var createDto = new CreateProductDto { Name = "Delete Me", Price = 10, SKU = $"SKU-{Guid.NewGuid():N}", CategoryId = categoryId };
            var createResp = await _client.PostAsJsonAsync("/api/products", createDto);
            var createdProduct = await createResp.Content.ReadFromJsonAsync<ProductDto>();
            var productId = createdProduct!.Id;

            var deleteResp = await _client.DeleteAsync($"/api/products/{productId}");
            Assert.Equal(HttpStatusCode.OK, deleteResp.StatusCode);
            
            var content = await deleteResp.Content.ReadAsStringAsync();
            Assert.Contains("\"success\":true", content);

            var getResp = await _client.GetAsync($"/api/products/{productId}");
            Assert.Equal(HttpStatusCode.NotFound, getResp.StatusCode);
        }

        [Fact]
        public async Task DuplicateSkuIsRejected_ReturnsBadRequest()
        {
            var token = await GetAdminTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var categoryId = await GetOrCreateCategoryAsync();
            var sku = $"SKU-{Guid.NewGuid():N}";
            var createDto = new CreateProductDto { Name = "Prod 1", Price = 10, SKU = sku, CategoryId = categoryId };
            
            await _client.PostAsJsonAsync("/api/products", createDto);
            
            // Try again with same SKU
            var createDto2 = new CreateProductDto { Name = "Prod 2", Price = 20, SKU = sku, CategoryId = categoryId };
            var response2 = await _client.PostAsJsonAsync("/api/products", createDto2);
            
            Assert.Equal(HttpStatusCode.BadRequest, response2.StatusCode);
        }

        [Fact]
        public async Task BasicProductSearch_FiltersCorrectly()
        {
            var token = await GetAdminTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            _client.DefaultRequestHeaders.Add("X-Idempotency-Key", Guid.NewGuid().ToString());

            var categoryId = await GetOrCreateCategoryAsync();
            var searchTargetName = $"UniqueName_{Guid.NewGuid():N}";
            
            var createDto1 = new CreateProductDto { Name = searchTargetName, Price = 10, SKU = $"SKU-{Guid.NewGuid():N}", CategoryId = categoryId };
            await _client.PostAsJsonAsync("/api/products", createDto1);

            var searchResp = await _client.GetAsync($"/api/products/search?q={searchTargetName}");
            Assert.Equal(HttpStatusCode.OK, searchResp.StatusCode);
            
            var results = await searchResp.Content.ReadFromJsonAsync<List<ProductDto>>();
            Assert.NotNull(results);
            Assert.Single(results);
            Assert.Equal(searchTargetName, results[0].Name);
        }
    }
}
