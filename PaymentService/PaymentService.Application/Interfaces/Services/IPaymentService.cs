using System;
using System.Threading.Tasks;
using PaymentService.Application.DTOs.Payments;

namespace PaymentService.Application.Interfaces.Services
{
    public interface IPaymentService
    {
        Task<PaymentDto> CreatePaymentAsync(Guid userId, CreatePaymentDto dto, string idempotencyKey, string requestPath);
        Task<PaymentDto> ProcessPaymentAsync(Guid userId, Guid paymentId);
        Task<PaymentDto> GetOrderPaymentAsync(Guid userId, Guid orderId);
        Task<PaymentDto?> GetInternalOrderPaymentAsync(Guid orderId);
        Task<PaymentDto> InitiateRefundAsync(Guid orderId);
        Task<PaymentDto> CompleteRefundAsync(Guid adminUserId, Guid paymentId, string idempotencyKey, string requestPath);
    }
}
