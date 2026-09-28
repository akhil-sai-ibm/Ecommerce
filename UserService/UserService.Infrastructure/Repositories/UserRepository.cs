using System;
using System.Threading.Tasks;
using UserService.Application.Interfaces.Repositories;
using UserService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using UserService.Infrastructure.Data;
using System.Linq;

namespace UserService.Infrastructure.Repositories
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(UserServiceDbContext context) : base(context)
        {
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _dbSet.Include(u => u.Role).AsNoTracking().FirstOrDefaultAsync(u => u.Email == email).ConfigureAwait(false);
        }

        public async Task<User?> GetWithRoleAsync(Guid userId)
        {
            return await _dbSet.Include(u => u.Role)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId).ConfigureAwait(false);
        }

        public async Task<System.Collections.Generic.IEnumerable<User>> GetUsersInRoleAsync(string roleName)
        {
            return await _dbSet.Include(u => u.Role)
                               .Where(u => u.Role != null && u.Role.Name == roleName)
                               .AsNoTracking()
                               .ToListAsync()
                               .ConfigureAwait(false);
        }
    }
}
