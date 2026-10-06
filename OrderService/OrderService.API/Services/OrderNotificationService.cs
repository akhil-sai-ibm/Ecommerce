using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using OrderService.API.Hubs;
using OrderService.Application.Interfaces.Services;

namespace OrderService.API.Services
{
    public class OrderNotificationService : IOrderNotificationService
    {
        private readonly IHubContext<OrderHub> _hubContext;

        public OrderNotificationService(IHubContext<OrderHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendOrderConfirmedNotificationAsync(Guid userId, Guid orderId)
        {
            await _hubContext.Clients.Group($"User_{userId}").SendAsync("OrderConfirmed", orderId);
        }

        public async Task SendOrderStatusUpdatedNotificationAsync(Guid userId, Guid orderId, int newStatus)
        {
            await _hubContext.Clients.Group($"User_{userId}").SendAsync("OrderStatusUpdated", new object[] { orderId, newStatus });
        }
    }
}
