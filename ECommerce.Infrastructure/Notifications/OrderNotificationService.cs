using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ECommerce.Application.Interfaces.Services;
using ECommerce.Domain.Enums;

namespace ECommerce.Infrastructure.Notifications
{
    /// <summary>
    /// Order notification service using dependency injection.
    /// The actual SignalR implementation is provided at the API layer.
    /// </summary>
    public class OrderNotificationService : IOrderNotificationService
    {
        private readonly ILogger<OrderNotificationService> _logger;
        private readonly Action<Guid, Guid, OrderStatus>? _notificationCallback;
        private readonly Action<Guid, Guid>? _cancellationCallback;

        public OrderNotificationService(
            ILogger<OrderNotificationService> logger,
            Action<Guid, Guid, OrderStatus>? notificationCallback = null,
            Action<Guid, Guid>? cancellationCallback = null)
        {
            _logger = logger;
            _notificationCallback = notificationCallback;
            _cancellationCallback = cancellationCallback;
        }

        public async Task NotifyOrderStatusChangedAsync(Guid userId, Guid orderId, OrderStatus newStatus)
        {
            try
            {
                _logger.LogInformation(
                    "Order status notification prepared for user {UserId} for order {OrderId} with status {Status}",
                    userId, orderId, newStatus);

                // Invoke callback if provided (from SignalR hub at API layer)
                if (_notificationCallback != null)
                {
                    await Task.Run(() => _notificationCallback(userId, orderId, newStatus))
                        .ConfigureAwait(false);
                }
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
                    "Order cancellation notification prepared for user {UserId} for order {OrderId}",
                    userId, orderId);

                // Invoke callback if provided
                if (_cancellationCallback != null)
                {
                    await Task.Run(() => _cancellationCallback(userId, orderId))
                        .ConfigureAwait(false);
                }
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
