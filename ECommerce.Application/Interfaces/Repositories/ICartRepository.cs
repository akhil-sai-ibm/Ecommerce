using System;
using System.Threading.Tasks;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Interfaces.Repositories
{
    public interface ICartRepository : IGenericRepository<Cart>
    {
        Task<Cart?> GetByUserIdAsync(Guid userId);

        Task<Cart?> GetWithItemsAsync(Guid cartId);

        Task RemoveCartItemsByProductIdAsync(Guid productId);

    }
}
