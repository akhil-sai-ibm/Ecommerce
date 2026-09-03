using System;
using System.Threading.Tasks;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Interfaces.Repositories
{
    public interface IPaymentRepository : IGenericRepository<Payment>
    {
        Task<Payment?> GetByOrderIdAsync(Guid orderId);

        Task<Payment?> GetByTransactionIdAsync(string transactionId);
    }
}
