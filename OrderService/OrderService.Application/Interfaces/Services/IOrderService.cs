using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OrderService.Application.DTOs.Order;

namespace OrderService.Application.Interfaces.Services
{
    public interface IOrderService
    {
        Task<OrderDto> CreateOrderAsync(Guid userId, CreateOrderDto createOrderDto, string idempotencyKey, string requestPath);
        Task<OrderDto> GetOrderAsync(Guid id, Guid userId);
        Task<IEnumerable<OrderDto>> GetUserOrdersAsync(Guid userId);
    }
}
