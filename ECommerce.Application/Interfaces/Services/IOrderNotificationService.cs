using System;
using System.Threading.Tasks;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Interfaces.Services
{
    /// <summary>
    /// Service abstraction for sending real-time order notifications to customers.
    /// Implementations should use SignalR or other real-time mechanisms.
    /// </summary>
    public interface IOrderNotificationService
    {
        /// <summary>
        /// Notify a customer about an order status change.
        /// </summary>
        /// <param name="userId">The customer ID receiving the notification</param>
        /// <param name="orderId">The order ID being updated</param>
        /// <param name="newStatus">The new order status</param>
        Task NotifyOrderStatusChangedAsync(Guid userId, Guid orderId, OrderStatus newStatus);

        /// <summary>
        /// Notify a customer about order cancellation.
        /// </summary>
        Task NotifyOrderCancelledAsync(Guid userId, Guid orderId);
    }
}
