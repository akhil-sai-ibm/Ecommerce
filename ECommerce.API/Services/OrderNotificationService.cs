using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using ECommerce.Application.Interfaces.Services;
using ECommerce.Domain.Enums;
using ECommerce.API.Hubs;
using ECommerce.Application.DTOs.Orders;

namespace ECommerce.API.Services
{
    /// <summary>
    /// Order notification service using SignalR.
    /// Provides real-time updates to connected clients.
    /// </summary>
    public class OrderNotificationService : IOrderNotificationService
    {
        private readonly IHubContext<OrderNotificationHub> _hubContext;
        private readonly ILogger<OrderNotificationService> _logger;

        public OrderNotificationService(
            IHubContext<OrderNotificationHub> hubContext,
            ILogger<OrderNotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task NotifyOrderStatusChangedAsync(Guid userId, Guid orderId, OrderStatus newStatus)
        {
            try
            {
                _logger.LogInformation(
                    "Sending order status notification to user {UserId} for order {OrderId} with status {Status}",
                    userId, orderId, newStatus);

                var payload = new OrderNotificationDto
                {
                    OrderId = orderId,
                    NewStatus = newStatus.ToString(),
                    Timestamp = DateTime.UtcNow,
                    Message = $"Your order status has been updated to {newStatus}."
                };

                await _hubContext.Clients.User(userId.ToString())
                    .SendAsync("ReceiveOrderStatusUpdate", payload)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error sending order status notification to user {UserId} for order {OrderId}",
                    userId, orderId);
            }
        }

        public async Task NotifyOrderCancelledAsync(Guid userId, Guid orderId)
        {
            try
            {
                _logger.LogInformation(
                    "Sending order cancellation notification to user {UserId} for order {OrderId}",
                    userId, orderId);

                var payload = new OrderNotificationDto
                {
                    OrderId = orderId,
                    NewStatus = OrderStatus.Cancelled.ToString(),
                    Timestamp = DateTime.UtcNow,
                    Message = "Your order has been cancelled."
                };

                await _hubContext.Clients.User(userId.ToString())
                    .SendAsync("ReceiveOrderStatusUpdate", payload)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error sending order cancellation notification to user {UserId} for order {OrderId}",
                    userId, orderId);
            }
        }
    }
}
