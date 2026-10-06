using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderService.Application.Interfaces.Services;
using OrderService.Domain.Entities;
using OrderService.Infrastructure.Data;

namespace OrderService.API.BackgroundServices
{
    public class OrderExpirationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OrderExpirationBackgroundService> _logger;

        public OrderExpirationBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<OrderExpirationBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessExpiredOrdersAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing expired orders");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task ProcessExpiredOrdersAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<OrderServiceDbContext>();
            var clock = scope.ServiceProvider.GetRequiredService<ISystemClock>();

            var expirationThreshold = clock.UtcNow.AddMinutes(-15);

            var expiredOrders = await dbContext.Orders
                .Where(o => (o.Status == 0 || o.Status == 1) && o.CreatedAt < expirationThreshold)
                .ToListAsync(stoppingToken);

            foreach (var order in expiredOrders)
            {
                try
                {
                    // Double check state
                    var currentOrder = await dbContext.Orders
                        .Where(o => o.Id == order.Id && (o.Status == 0 || o.Status == 1) && o.CreatedAt < expirationThreshold)
                        .FirstOrDefaultAsync(stoppingToken);

                    if (currentOrder == null)
                        continue;

                    currentOrder.Status = 5; // Cancelled
                    currentOrder.UpdatedAt = clock.UtcNow;

                    var outboxPayload = new
                    {
                        OrderId = currentOrder.Id,
                        ReservationId = currentOrder.ReservationId,
                        IsPaymentSuccessful = currentOrder.IsPaymentSuccessful
                    };

                    var outboxMessage = new OutboxMessage
                    {
                        Id = Guid.NewGuid(),
                        EventType = "OrderCancelled",
                        Payload = JsonSerializer.Serialize(outboxPayload),
                        CreatedAt = clock.UtcNow,
                        Processed = false
                    };

                    dbContext.OutboxMessages.Add(outboxMessage);

                    await dbContext.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation($"Order {currentOrder.Id} expired and cancelled successfully.");
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogWarning(ex, $"Concurrency exception cancelling order {order.Id}. Order was updated by another process.");
                    // Reset state in DbContext to avoid exception on next iteration if we don't recreate dbContext
                    dbContext.Entry(order).State = EntityState.Detached;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to expire order {order.Id}");
                    dbContext.Entry(order).State = EntityState.Detached;
                }
            }
        }
    }
}
