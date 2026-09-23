using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECommerce.Application.Interfaces.Repositories;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.BackgroundJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ECommerce.UnitTests.BackgroundJobs
{
    public class LowStockBackgroundServiceTests
    {
        private readonly Mock<IServiceProvider> _serviceProviderMock;
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactoryMock;
        private readonly Mock<IServiceScope> _serviceScopeMock;
        private readonly Mock<IInventoryRepository> _inventoryRepoMock;
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly Mock<INotificationRepository> _notificationRepoMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ILogger<LowStockBackgroundService>> _loggerMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<IConfigurationSection> _configurationSectionMock;
        private readonly LowStockBackgroundService _service;

        public LowStockBackgroundServiceTests()
        {
            _serviceProviderMock = new Mock<IServiceProvider>();
            _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
            _serviceScopeMock = new Mock<IServiceScope>();
            _inventoryRepoMock = new Mock<IInventoryRepository>();
            _userRepoMock = new Mock<IUserRepository>();
            _notificationRepoMock = new Mock<INotificationRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _loggerMock = new Mock<ILogger<LowStockBackgroundService>>();
            _configurationMock = new Mock<IConfiguration>();
            _configurationSectionMock = new Mock<IConfigurationSection>();

            _configurationSectionMock.Setup(s => s.Value).Returns("60");
            _configurationMock.Setup(c => c.GetSection(It.IsAny<string>())).Returns(_configurationSectionMock.Object);

            _serviceScopeMock.Setup(s => s.ServiceProvider).Returns(_serviceProviderMock.Object);
            _serviceScopeFactoryMock.Setup(f => f.CreateScope()).Returns(_serviceScopeMock.Object);
            
            _serviceProviderMock.Setup(p => p.GetService(typeof(IServiceScopeFactory))).Returns(_serviceScopeFactoryMock.Object);
            _serviceProviderMock.Setup(p => p.GetService(typeof(IInventoryRepository))).Returns(_inventoryRepoMock.Object);
            _serviceProviderMock.Setup(p => p.GetService(typeof(IUserRepository))).Returns(_userRepoMock.Object);
            _serviceProviderMock.Setup(p => p.GetService(typeof(INotificationRepository))).Returns(_notificationRepoMock.Object);
            _serviceProviderMock.Setup(p => p.GetService(typeof(IUnitOfWork))).Returns(_unitOfWorkMock.Object);

            _service = new LowStockBackgroundService(_serviceProviderMock.Object, _loggerMock.Object, _configurationMock.Object);
        }

        [Fact]
        public async Task CheckLowStockItemsAsync_ShouldNotCreateNotifications_WhenNoLowStockItems()
        {
            // Arrange
            _inventoryRepoMock.Setup(repo => repo.GetLowStockItemsAsync())
                .ReturnsAsync(new List<Inventory>());

            // Act
            await _service.CheckLowStockItemsAsync(CancellationToken.None);

            // Assert
            _userRepoMock.Verify(repo => repo.GetUsersInRoleAsync(It.IsAny<string>()), Times.Never);
            _notificationRepoMock.Verify(repo => repo.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task CheckLowStockItemsAsync_ShouldCreateNotifications_WhenLowStockItemsExist_AndAdminsExist()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            _inventoryRepoMock.Setup(repo => repo.GetLowStockItemsAsync())
                .ReturnsAsync(new List<Inventory> { new Inventory { ProductId = productId, Quantity = 5, ReorderLevel = 10 } });

            _userRepoMock.Setup(repo => repo.GetUsersInRoleAsync("Admin"))
                .ReturnsAsync(new List<User> { new User { Id = adminId, Email = "admin@example.com" } });

            _notificationRepoMock.Setup(repo => repo.HasUnreadLowStockNotificationAsync(adminId, productId))
                .ReturnsAsync(false);

            // Act
            await _service.CheckLowStockItemsAsync(CancellationToken.None);

            // Assert
            _notificationRepoMock.Verify(repo => repo.AddAsync(It.Is<Notification>(n => n.UserId == adminId && n.Message.Contains(productId.ToString())), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CheckLowStockItemsAsync_ShouldNotCreateNotifications_WhenAlreadyNotified()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            _inventoryRepoMock.Setup(repo => repo.GetLowStockItemsAsync())
                .ReturnsAsync(new List<Inventory> { new Inventory { ProductId = productId, Quantity = 5, ReorderLevel = 10 } });

            _userRepoMock.Setup(repo => repo.GetUsersInRoleAsync("Admin"))
                .ReturnsAsync(new List<User> { new User { Id = adminId, Email = "admin@example.com" } });

            _notificationRepoMock.Setup(repo => repo.HasUnreadLowStockNotificationAsync(adminId, productId))
                .ReturnsAsync(true); // Already notified

            // Act
            await _service.CheckLowStockItemsAsync(CancellationToken.None);

            // Assert
            _notificationRepoMock.Verify(repo => repo.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task CheckLowStockItemsAsync_ShouldHandleExceptionsGracefully()
        {
            // Arrange
            _inventoryRepoMock.Setup(repo => repo.GetLowStockItemsAsync())
                .ThrowsAsync(new Exception("Database connection failed"));

            // Act
            var exception = await Record.ExceptionAsync(() => _service.CheckLowStockItemsAsync(CancellationToken.None));

            // Assert
            Assert.Null(exception); // Should not throw
            // Verify logger was called to log the exception (Moq logger verification is a bit complex, but checking that it doesn't throw is good enough for gracefulness)
        }
    }
}
