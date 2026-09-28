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
        private readonly IPaymentRepository _paymentRepository;
        private readonly IProductServiceClient _productServiceClient;
        private readonly IOrderNotificationService _notificationService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<OutboxService> _logger;

        public OutboxService(
            IOutboxRepository outboxRepository,
            IOrderRepository orderRepository,
            IPaymentRepository paymentRepository,
            IProductServiceClient productServiceClient,
            IOrderNotificationService notificationService,
            IUnitOfWork unitOfWork,
            ILogger<OutboxService> logger)
        {
            _outboxRepository = outboxRepository;
            _orderRepository = orderRepository;
            _paymentRepository = paymentRepository;
            _productServiceClient = productServiceClient;
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

                        // 1. Process Payment
                        var payment = new Payment
                        {
                            Id = Guid.NewGuid(),
                            OrderId = orderId,
                            Amount = order.TotalAmount,
                            Status = "Success", // Dummy success
                            ProcessedAt = DateTime.UtcNow,
                            TransactionId = Guid.NewGuid().ToString()
                        };
                        
                        await _paymentRepository.AddAsync(payment);
                        order.PaymentId = payment.Id;
                        
                        // 2. Confirm Inventory in ProductService
                        await _productServiceClient.ConfirmInventoryAsync(reservationId);
                        
                        // 3. Update Order Status
                        order.Status = 2; // Processing (or confirmed)
                        await _orderRepository.UpdateAsync(order);

                        // 4. Send SignalR Notification
                        await _notificationService.SendOrderConfirmedNotificationAsync(userId, orderId);

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
