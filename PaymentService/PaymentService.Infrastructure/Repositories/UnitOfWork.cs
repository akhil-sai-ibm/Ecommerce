using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Infrastructure.Data;
using System.Threading.Tasks;

namespace PaymentService.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly PaymentServiceDbContext _context;

        public UnitOfWork(PaymentServiceDbContext context)
        {
            _context = context;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
