using System;
using System.Threading.Tasks;
using UserService.Domain.Entities;

namespace UserService.Application.Interfaces.Repositories
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User?> GetByEmailAsync(string email);

        Task<User?> GetWithRoleAsync(Guid userId);

        Task<IEnumerable<User>> GetUsersInRoleAsync(string roleName);
    }
}
