using System;
using System.Threading.Tasks;

namespace OrderService.Application.Interfaces.Services
{
    public interface IOrderNotificationService
    {
        Task SendOrderConfirmedNotificationAsync(Guid userId, Guid orderId);
        Task SendOrderStatusUpdatedNotificationAsync(Guid userId, Guid orderId, int newStatus);
    }
}
