using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Application.DTOs.Orders;

namespace ECommerce.Application.Interfaces.Services
{
    public interface IOrderService
    {
        Task<OrderDto?> GetByIdAsync(Guid id);
        Task<IEnumerable<OrderDto>> GetByUserIdAsync(Guid userId);
        Task<IEnumerable<OrderDto>> GetAllAsync();
        Task<OrderDto> CreateOrderFromCartAsync(Guid userId, CheckoutRequestDto request, string? idempotencyKey = null);
        Task UpdateOrderStatusAsync(Guid orderId, int status);
        Task CancelOrderAsync(Guid orderId);
    }
}
