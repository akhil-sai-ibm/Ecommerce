using System;
using System.Threading.Tasks;
using ECommerce.Application.Interfaces.Repositories;
using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using System.Linq;

namespace ECommerce.Infrastructure.Repositories
{
    public class NotificationRepository : GenericRepository<Notification>, INotificationRepository
    {
        public NotificationRepository(ECommerceDbContext context) : base(context)
        {
        }

        public async Task<bool> HasUnreadLowStockNotificationAsync(Guid userId, Guid productId)
        {
            var searchString = $"Low stock alert for Product {productId}";
            return await _dbSet.AnyAsync(n => n.UserId == userId && 
                                              !n.IsRead && 
                                              n.Message.Contains(searchString)).ConfigureAwait(false);
        }
    }
}
