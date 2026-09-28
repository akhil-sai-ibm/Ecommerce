using OrderService.Application.Interfaces.Repositories;
using OrderService.Infrastructure.Data;
using System.Threading.Tasks;

namespace OrderService.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly OrderServiceDbContext _context;

        public UnitOfWork(OrderServiceDbContext context)
        {
            _context = context;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
