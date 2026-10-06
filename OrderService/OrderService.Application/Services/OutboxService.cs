using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OrderService.Application.Interfaces.Clients;
using OrderService.Application.Interfaces.Repositories;
using OrderService.Application.Interfaces.Services;
using OrderService.Domain.Entities;

namespace OrderService.Application.Services
{
    public class OutboxService : IOutboxService
    {
        private readonly IOutboxRepository _outboxRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IProductServiceClient _productServiceClient;
        private readonly IPaymentServiceClient _paymentServiceClient;
        private readonly IOrderNotificationService _notificationService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<OutboxService> _logger;

        public OutboxService(
            IOutboxRepository outboxRepository,
            IOrderRepository orderRepository,
            IProductServiceClient productServiceClient,
            IPaymentServiceClient paymentServiceClient,
            IOrderNotificationService notificationService,
            IUnitOfWork unitOfWork,
            ILogger<OutboxService> logger)
        {
            _outboxRepository = outboxRepository;
            _orderRepository = orderRepository;
            _productServiceClient = productServiceClient;
            _paymentServiceClient = paymentServiceClient;
            _notificationService = notificationService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task ProcessOutboxMessagesAsync()
        {
            var messages = await _outboxRepository.GetUnprocessedMessagesAsync(10);
            
            foreach (var message in messages)
            {
                try
                {
                    if (message.EventType == "OrderCreated")
                    {
                        var payload = JsonSerializer.Deserialize<JsonElement>(message.Payload);
                        var orderId = payload.GetProperty("OrderId").GetGuid();
                        var reservationId = payload.GetProperty("ReservationId").GetGuid();
                        var userId = payload.GetProperty("UserId").GetGuid();

                        var order = await _orderRepository.GetByIdAsync(orderId);
                        if (order == null) continue;

                        // 1. Confirm Inventory in ProductService
                        await _productServiceClient.ConfirmInventoryAsync(reservationId);
                        
                        // 2. Send SignalR Notification
                        await _notificationService.SendOrderConfirmedNotificationAsync(userId, orderId);

                        message.Processed = true;
                        message.ProcessedAt = DateTime.UtcNow;
                    }
                    else if (message.EventType == "OrderCancelled")
                    {
                        var payload = JsonSerializer.Deserialize<JsonElement>(message.Payload);
                        // Make sure we handle old orders where ReservationId might be missing in DB,
                        // but since OrderCancelled is created WITH ReservationId, it will be in the payload if available.
                        if (payload.TryGetProperty("ReservationId", out var resIdElement) && resIdElement.ValueKind != JsonValueKind.Null)
                        {
                            var reservationId = resIdElement.GetGuid();
                            await _productServiceClient.ReleaseInventoryAsync(reservationId);
                        }
                        
                        if (payload.TryGetProperty("IsPaymentSuccessful", out var isPaymentElement) && isPaymentElement.GetBoolean())
                        {
                            var orderId = payload.GetProperty("OrderId").GetGuid();
                            await _paymentServiceClient.InitiateRefundAsync(orderId);
                        }
                        
                        message.Processed = true;
                        message.ProcessedAt = DateTime.UtcNow;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to process outbox message {message.Id}");
                    message.RetryCount++;
                }

                await _outboxRepository.UpdateAsync(message);
                await _unitOfWork.SaveChangesAsync();
            }
        }
    }
}
