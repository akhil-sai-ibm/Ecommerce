using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using ECommerce.Application.Interfaces.Services;
using ECommerce.Domain.Entities;
using ECommerce.Application.DTOs.Orders;
using Xunit;
using ECommerce.API;

namespace ECommerce.IntegrationTests
{
    public class SignalRIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public SignalRIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task CanConnectToHub_AndReceiveNotification()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var orderId = Guid.NewGuid();
            var testUser = new User 
            { 
                Id = userId, 
                Email = "testsignalr@example.com", 
                PasswordHash = "hash", 
                Role = new Role { Name = "User" }, 
                FirstName = "Test", 
                LastName = "User" 
            };
            
            // Get a token using ITokenService
            string token = string.Empty;
            using (var scope = _factory.Services.CreateScope())
            {
                var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
                token = await tokenService.GenerateTokenAsync(testUser);
            }

            // Create HubConnection
            var connection = new HubConnectionBuilder()
                .WithUrl("http://localhost/hubs/orders", options =>
                {
                    options.Transports = HttpTransportType.LongPolling;
                    options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                    options.AccessTokenProvider = () => Task.FromResult(token);
                })
                .Build();

            var tcs = new TaskCompletionSource<OrderNotificationDto>();
            
            // Listen to the event
            connection.On<OrderNotificationDto>("ReceiveOrderStatusUpdate", (payload) =>
            {
                tcs.TrySetResult(payload);
            });

            // Act
            await connection.StartAsync();

            // Resolve the OrderNotificationService from DI and trigger it, 
            // since we're verifying the hub routing works, we don't necessarily need an HTTP call,
            // we can just call the service method directly inside the app.
            using (var scope = _factory.Services.CreateScope())
            {
                var notificationService = scope.ServiceProvider.GetRequiredService<IOrderNotificationService>();
                await notificationService.NotifyOrderStatusChangedAsync(userId, orderId, Domain.Enums.OrderStatus.Shipped);
            }

            // Assert
            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(5000));
            Assert.Equal(tcs.Task, completedTask); // Ensure we didn't timeout
            
            var result = await tcs.Task;
            Assert.NotNull(result);
            Assert.Equal(orderId, result.OrderId);
            Assert.Equal("Shipped", result.NewStatus);

            await connection.StopAsync();
        }
    }
}
