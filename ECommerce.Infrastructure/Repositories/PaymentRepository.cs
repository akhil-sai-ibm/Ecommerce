using System;
using System.Threading.Tasks;
using ECommerce.Application.Interfaces.Repositories;
using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;

namespace ECommerce.Infrastructure.Repositories
{
    public class PaymentRepository : GenericRepository<Payment>, IPaymentRepository
    {
        public PaymentRepository(ECommerceDbContext context) : base(context)
        {
        }

        public async Task<Payment?> GetByOrderIdAsync(Guid orderId)
        {
            return await _dbSet.AsNoTracking().FirstOrDefaultAsync(p => p.OrderId == orderId).ConfigureAwait(false);
        }

        public async Task<Payment?> GetByTransactionIdAsync(string transactionId)
        {
            return await _dbSet.AsNoTracking().FirstOrDefaultAsync(p => p.TransactionId == transactionId).ConfigureAwait(false);
        }
    }
}
