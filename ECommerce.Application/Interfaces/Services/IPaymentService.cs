using System;
using System.Threading.Tasks;
using ECommerce.Application.DTOs.Payments;

namespace ECommerce.Application.Interfaces.Services
{
    public interface IPaymentService
    {
        Task<PaymentDto?> GetByIdAsync(Guid id);
        Task<PaymentDto?> GetByOrderIdAsync(Guid orderId);
        Task<PaymentDto> CreateAsync(CreatePaymentDto dto);
        Task UpdateStatusAsync(Guid paymentId, string status);
        Task<PaymentDto> ProcessPaymentAsync(Guid paymentId);
    }
}
