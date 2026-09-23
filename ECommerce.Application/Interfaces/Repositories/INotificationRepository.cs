using System;
using System.Threading.Tasks;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Interfaces.Repositories
{
    public interface INotificationRepository : IGenericRepository<Notification>
    {
        Task<bool> HasUnreadLowStockNotificationAsync(Guid userId, Guid productId);
    }
}
