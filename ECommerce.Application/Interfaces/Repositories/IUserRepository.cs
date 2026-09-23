using System;
using System.Threading.Tasks;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Interfaces.Repositories
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User?> GetByEmailAsync(string email);

        Task<User?> GetWithOrdersAsync(Guid userId);
        
        Task<User?> GetWithRoleAsync(Guid userId);

        Task<User?> GetWithCartAsync(Guid userId);

        Task<IEnumerable<User>> GetUsersInRoleAsync(string roleName);
    }
}
