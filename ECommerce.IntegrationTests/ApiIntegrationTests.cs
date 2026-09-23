using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ECommerce.API;
using ECommerce.Application.DTOs.Auth;
using ECommerce.Infrastructure.Data;
using Xunit;
using ECommerce.Domain.Entities;
using System.Linq;

namespace ECommerce.IntegrationTests
{
    public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private static readonly Guid AdminRoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        public ApiIntegrationTests(WebApplicationFactory<Program> factory)
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

        private async Task<AuthResponse?> RegisterAndLoginAsync(RegisterRequest request)
        {
            // Register
            var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", request);
            registerResponse.EnsureSuccessStatusCode();

            // Login
            var loginRequest = new LoginRequest { Email = request.Email, Password = request.Password };
            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
            loginResponse.EnsureSuccessStatusCode();

            return await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        }

        [Fact]
        public async Task Register_WithValidData_ReturnsCreated()
        {
            var request = CreateUniqueRegisterRequest();
            var response = await _client.PostAsJsonAsync("/api/auth/register", request);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync<AuthResponse>();
            Assert.NotNull(content);
            Assert.NotEmpty(content.Token);
            Assert.Equal(request.Email.ToLowerInvariant(), content.User.Email);
        }

        [Fact]
        public async Task Register_WithDuplicateEmail_ReturnsBadRequestOrConflict()
        {
            var request = CreateUniqueRegisterRequest();
            // First registration
            await _client.PostAsJsonAsync("/api/auth/register", request);
            
            // Second registration
            var response = await _client.PostAsJsonAsync("/api/auth/register", request);

            // Our exception middleware translates DomainException to 400 Bad Request
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Register_WithInvalidData_ReturnsBadRequest()
        {
            var request = new RegisterRequest
            {
                FirstName = "",
                LastName = "",
                Email = "",
                Password = ""
            };

            var response = await _client.PostAsJsonAsync("/api/auth/register", request);
            
            // Either domain exception or model validation error
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsOkAndToken()
        {
            var request = CreateUniqueRegisterRequest();
            await _client.PostAsJsonAsync("/api/auth/register", request);

            var loginRequest = new LoginRequest { Email = request.Email, Password = request.Password };
            var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync<AuthResponse>();
            Assert.NotNull(content);
            Assert.NotEmpty(content.Token);
            Assert.Equal(request.Email.ToLowerInvariant(), content.User.Email);
        }

        [Fact]
        public async Task Login_WithInvalidCredentials_ReturnsBadRequest()
        {
            var request = CreateUniqueRegisterRequest();
            await _client.PostAsJsonAsync("/api/auth/register", request);

            var loginRequest = new LoginRequest { Email = request.Email, Password = "WrongPassword!" };
            var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

            // Our exception middleware translates DomainException to 400 Bad Request
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UnauthenticatedAccess_ToProtectedEndpoint_ReturnsUnauthorized()
        {
            var response = await _client.GetAsync("/api/orders/me");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CustomerAccess_ToAdminEndpoint_ReturnsForbidden()
        {
            // Create a Customer user
            var request = CreateUniqueRegisterRequest();
            var authResponse = await RegisterAndLoginAsync(request);

            // Call Admin endpoint
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"/api/orders/user/{Guid.NewGuid()}");
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResponse!.Token);
            var response = await _client.SendAsync(requestMessage);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task AdminAccess_ToAdminEndpoint_ReturnsSuccess()
        {
            // 1. Create a User
            var request = CreateUniqueRegisterRequest();
            var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", request);
            registerResponse.EnsureSuccessStatusCode();
            
            var regData = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

            // 2. Promote to Admin via DB Context
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();
                var user = await dbContext.Users.FindAsync(regData!.User.Id);
                if (user != null)
                {
                    user.RoleId = AdminRoleId;
                    await dbContext.SaveChangesAsync();
                }
            }

            // 3. Login to get Admin Token
            var loginRequest = new LoginRequest { Email = request.Email, Password = request.Password };
            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
            var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

            // 4. Call Admin endpoint
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"/api/orders/user/{regData!.User.Id}");
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResponse!.Token);
            var response = await _client.SendAsync(requestMessage);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task CustomerAccess_ToOtherCustomerResource_ReturnsForbidden()
        {
            // 1. Create Customer A
            var requestA = CreateUniqueRegisterRequest();
            var authA = await RegisterAndLoginAsync(requestA);

            // 2. Create Customer B
            var requestB = CreateUniqueRegisterRequest();
            var authB = await RegisterAndLoginAsync(requestB);

            // 3. Customer A creates an order (assuming checkout endpoint exists or we can just seed one)
            // Wait, we don't need to create an actual order if GetById returns 404 vs 403.
            // Let's create an order for A in DB directly so we can test GetById for it.
            var orderId = Guid.NewGuid();
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();
                dbContext.Orders.Add(new Order 
                { 
                    Id = orderId, 
                    UserId = authA!.User.Id, 
                    TotalAmount = 100, 
                    Status = Domain.Enums.OrderStatus.Pending, 
                    CreatedAt = DateTime.UtcNow 
                });
                await dbContext.SaveChangesAsync();
            }

            // 4. Customer B tries to access Customer A's order
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"/api/orders/{orderId}");
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authB!.Token);
            var response = await _client.SendAsync(requestMessage);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
