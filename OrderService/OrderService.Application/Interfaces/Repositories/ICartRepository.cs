using OrderService.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace OrderService.Application.Interfaces.Repositories
{
    public interface ICartRepository
    {
        Task<Cart?> GetByUserIdAsync(Guid userId);
        Task<Cart> AddAsync(Cart cart);
        Task UpdateAsync(Cart cart);
    }
}
