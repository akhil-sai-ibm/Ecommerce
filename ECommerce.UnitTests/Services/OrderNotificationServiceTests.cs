using System;
using System.Threading;
using System.Threading.Tasks;
using ECommerce.API.Hubs;
using ECommerce.API.Services;
using ECommerce.Application.DTOs.Orders;
using ECommerce.Domain.Enums;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ECommerce.UnitTests.Services
{
    public class OrderNotificationServiceTests
    {
        private readonly Mock<IHubContext<OrderNotificationHub>> _mockHubContext;
        private readonly Mock<IHubClients> _mockClients;
        private readonly Mock<IClientProxy> _mockClientProxy;
        private readonly Mock<ILogger<OrderNotificationService>> _mockLogger;
        private readonly OrderNotificationService _service;

        public OrderNotificationServiceTests()
        {
            _mockHubContext = new Mock<IHubContext<OrderNotificationHub>>();
            _mockClients = new Mock<IHubClients>();
            _mockClientProxy = new Mock<IClientProxy>();
            _mockLogger = new Mock<ILogger<OrderNotificationService>>();

            _mockHubContext.Setup(h => h.Clients).Returns(_mockClients.Object);
            _mockClients.Setup(c => c.User(It.IsAny<string>())).Returns(_mockClientProxy.Object);

            _service = new OrderNotificationService(_mockHubContext.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task NotifyOrderStatusChangedAsync_SendsMessageToCorrectUserWithExpectedPayload()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var orderId = Guid.NewGuid();
            var newStatus = OrderStatus.Shipped;

            // Act
            await _service.NotifyOrderStatusChangedAsync(userId, orderId, newStatus);

            // Assert
            _mockClients.Verify(c => c.User(userId.ToString()), Times.Once);

            _mockClientProxy.Verify(p => p.SendCoreAsync(
                "ReceiveOrderStatusUpdate",
                It.Is<object[]>(args => 
                    args.Length == 1 &&
                    args[0] as OrderNotificationDto != null &&
                    ((OrderNotificationDto)args[0]).OrderId == orderId &&
                    ((OrderNotificationDto)args[0]).NewStatus == "Shipped" &&
                    ((OrderNotificationDto)args[0]).Timestamp > DateTime.UtcNow.AddSeconds(-10) && // roughly now
                    ((OrderNotificationDto)args[0]).Message.Contains("Shipped")
                ),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task NotifyOrderCancelledAsync_SendsMessageToCorrectUserWithExpectedPayload()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var orderId = Guid.NewGuid();

            // Act
            await _service.NotifyOrderCancelledAsync(userId, orderId);

            // Assert
            _mockClients.Verify(c => c.User(userId.ToString()), Times.Once);

            _mockClientProxy.Verify(p => p.SendCoreAsync(
                "ReceiveOrderStatusUpdate",
                It.Is<object[]>(args => 
                    args.Length == 1 &&
                    args[0] as OrderNotificationDto != null &&
                    ((OrderNotificationDto)args[0]).OrderId == orderId &&
                    ((OrderNotificationDto)args[0]).NewStatus == "Cancelled" &&
                    ((OrderNotificationDto)args[0]).Timestamp > DateTime.UtcNow.AddSeconds(-10) &&
                    ((OrderNotificationDto)args[0]).Message.Contains("cancelled")
                ),
                It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact]
        public async Task NotifyOrderStatusChangedAsync_WhenHubThrows_DoesNotThrow()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var orderId = Guid.NewGuid();
            var newStatus = OrderStatus.Processing;

            _mockClients.Setup(c => c.User(It.IsAny<string>())).Throws(new Exception("SignalR disconnected"));

            // Act
            var exception = await Record.ExceptionAsync(() => 
                _service.NotifyOrderStatusChangedAsync(userId, orderId, newStatus));

            // Assert
            Assert.Null(exception); // Should swallow exception and log it
            
            // Verify error was logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error sending order status notification")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.Once);
        }
    }
}
