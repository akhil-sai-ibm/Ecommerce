using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProductService.Application.Interfaces.Repositories;

namespace ProductService.API.BackgroundServices
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
            _logger.LogInformation("LowStockBackgroundService is starting in ProductService.");

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

                var lowStockItems = await inventoryRepo.GetLowStockItemsAsync();

                if (lowStockItems == null || !lowStockItems.Any())
                {
                    return;
                }

                _logger.LogInformation($"Found {lowStockItems.Count()} low stock items.");

                foreach (var item in lowStockItems)
                {
                    // As per UC3 architecture constraints, ProductService does not have access to 
                    // User or Notification repositories. It acts autonomously. 
                    // Therefore, we simply preserve the behavior via warning logs which could be ingested by a central log monitoring system (like ELK/Splunk).
                    _logger.LogWarning($"Low stock alert for Product {item.ProductId}. Quantity: {item.Quantity}.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while checking low stock items.");
            }
        }
    }
}
