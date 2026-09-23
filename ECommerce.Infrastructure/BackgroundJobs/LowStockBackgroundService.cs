using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECommerce.Application.Interfaces.Repositories;
using ECommerce.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.BackgroundJobs
{
    public class LowStockBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LowStockBackgroundService> _logger;
        private readonly int _checkIntervalSeconds;

        public LowStockBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<LowStockBackgroundService> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _checkIntervalSeconds = int.TryParse(configuration["BackgroundJobSettings:LowStockCheckIntervalSeconds"], out var interval) ? interval : 60;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("LowStockBackgroundService is starting.");

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_checkIntervalSeconds));

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await CheckLowStockItemsAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("LowStockBackgroundService execution is being canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while executing the LowStockBackgroundService.");
            }

            _logger.LogInformation("LowStockBackgroundService is stopping.");
        }

        public async Task CheckLowStockItemsAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                
                var inventoryRepo = scope.ServiceProvider.GetRequiredService<IInventoryRepository>();
                var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                var notificationRepo = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var lowStockItems = await inventoryRepo.GetLowStockItemsAsync();

                if (lowStockItems == null || !lowStockItems.Any())
                {
                    return;
                }

                _logger.LogInformation($"Found {lowStockItems.Count()} low stock items.");

                var admins = await userRepo.GetUsersInRoleAsync("Admin");
                if (admins == null || !admins.Any())
                {
                    _logger.LogWarning("No Admin users found. Cannot create low stock notifications.");
                    return;
                }

                bool hasNewNotifications = false;

                foreach (var item in lowStockItems)
                {
                    string message = $"Low stock alert for Product {item.ProductId}. Quantity: {item.Quantity}.";

                    foreach (var admin in admins)
                    {
                        var alreadyNotified = await notificationRepo.HasUnreadLowStockNotificationAsync(admin.Id, item.ProductId);
                        if (!alreadyNotified)
                        {
                            var notification = new Notification
                            {
                                UserId = admin.Id,
                                Message = message,
                                IsRead = false
                            };

                            await notificationRepo.AddAsync(notification, stoppingToken);
                            hasNewNotifications = true;
                            
                            _logger.LogInformation($"Created low stock notification for Admin {admin.Email} regarding Product {item.ProductId}.");
                        }
                    }
                }

                if (hasNewNotifications)
                {
                    await unitOfWork.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while checking low stock items.");
            }
        }
    }
}
