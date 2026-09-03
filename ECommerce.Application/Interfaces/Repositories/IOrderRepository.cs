using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Interfaces.Repositories
{
    public interface IOrderRepository : IGenericRepository<Order>
    {
        Task<Order?> GetByIdWithDetailsAsync(Guid orderId);

        Task<IEnumerable<Order>> GetByUserIdAsync(Guid userId);

        Task<IEnumerable<Order>> GetAllWithDetailsAsync();

        Task<IEnumerable<Order>> GetByStatusAsync(OrderStatus status);
    }
}
