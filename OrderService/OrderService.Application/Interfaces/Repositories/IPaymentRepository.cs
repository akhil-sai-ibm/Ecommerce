using OrderService.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace OrderService.Application.Interfaces.Repositories
{
    public interface IPaymentRepository
    {
        Task<Payment?> GetByOrderIdAsync(Guid orderId);
        Task<Payment> AddAsync(Payment payment);
        Task UpdateAsync(Payment payment);
    }
}
