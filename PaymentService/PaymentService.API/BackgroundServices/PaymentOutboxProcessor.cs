using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Interfaces.Clients;
using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Domain.Entities;

namespace PaymentService.API.BackgroundServices
{
    public class PaymentOutboxProcessor : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PaymentOutboxProcessor> _logger;
        private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(5);

        public PaymentOutboxProcessor(IServiceProvider serviceProvider, ILogger<PaymentOutboxProcessor> logger)
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
                    await ProcessMessagesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing outbox messages");
                }

                await Task.Delay(_pollInterval, stoppingToken);
            }
        }

        private async Task ProcessMessagesAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
            var orderServiceClient = scope.ServiceProvider.GetRequiredService<IOrderServiceClient>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var messages = await outboxRepository.GetUnprocessedMessagesAsync(20);

            foreach (var message in messages)
            {
                try
                {
                    if (message.EventType == "PaymentSucceeded")
                    {
                        var payload = JsonDocument.Parse(message.Payload);
                        var orderId = payload.RootElement.GetProperty("OrderId").GetGuid();

                        var success = await orderServiceClient.MarkPaymentSuccessfulAsync(orderId);
                        if (!success) throw new Exception("Failed to mark payment successful in OrderService.");
                        
                        message.Processed = true;
                        message.ProcessedAt = DateTime.UtcNow;
                    }
                    else if (message.EventType == "PaymentRefunded")
                    {
                        // To be implemented in Phase 3B
                        message.Processed = true;
                        message.ProcessedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        // Unknown event
                        message.Processed = true;
                        message.ProcessedAt = DateTime.UtcNow;
                    }

                    await outboxRepository.UpdateAsync(message);
                    await unitOfWork.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process outbox message {MessageId}", message.Id);
                    message.RetryCount++;
                    await outboxRepository.UpdateAsync(message);
                    await unitOfWork.SaveChangesAsync();
                }
            }
        }
    }
}
