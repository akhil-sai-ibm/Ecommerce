using PaymentService.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace PaymentService.Application.Interfaces.Repositories
{
    public interface IPaymentRepository
    {
        Task<Payment?> GetByIdAsync(Guid id);
        Task<Payment?> GetByOrderIdAsync(Guid orderId);
        Task<Payment> AddAsync(Payment payment);
        Task UpdateAsync(Payment payment);
        void Delete(Payment payment);
    }
}
