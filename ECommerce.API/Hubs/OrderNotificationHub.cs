using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace ECommerce.API.Hubs
{
    /// <summary>
    /// SignalR hub for real-time order notifications.
    /// Customers connect to receive updates about their orders.
    /// </summary>
    public class OrderNotificationHub : Hub
    {
        private readonly ILogger<OrderNotificationHub> _logger;

        public OrderNotificationHub(ILogger<OrderNotificationHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst("sub")?.Value ?? Context.User?.FindFirst("nameid")?.Value;
            _logger.LogInformation("OrderNotificationHub: User {UserId} connected. ConnectionId: {ConnectionId}", 
                userId ?? "Anonymous", Context.ConnectionId);

            await base.OnConnectedAsync().ConfigureAwait(false);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst("sub")?.Value ?? Context.User?.FindFirst("nameid")?.Value;
            _logger.LogInformation("OrderNotificationHub: User {UserId} disconnected. ConnectionId: {ConnectionId}", 
                userId ?? "Anonymous", Context.ConnectionId);

            if (exception != null)
            {
                _logger.LogError(exception, "OrderNotificationHub: User {UserId} disconnected with exception", userId ?? "Anonymous");
            }

            await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
        }

        /// <summary>
        /// Called by the server to send order status update to a connected customer.
        /// This method is not directly called by clients, but by the OrderNotificationService.
        /// </summary>
        public async Task SendOrderStatusUpdate(Guid orderId, string orderStatus)
        {
            await Clients.All.SendAsync("ReceiveOrderStatusUpdate", orderId, orderStatus).ConfigureAwait(false);
        }
    }
}
