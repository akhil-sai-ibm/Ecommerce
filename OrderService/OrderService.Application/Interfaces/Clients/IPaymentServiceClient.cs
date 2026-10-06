using System;
using System.Threading.Tasks;
using OrderService.Application.DTOs.Payments;

namespace OrderService.Application.Interfaces.Clients
{
    public interface IPaymentServiceClient
    {
        Task InitiateRefundAsync(Guid orderId);
        Task<PaymentDto?> GetPaymentByOrderIdAsync(Guid orderId);
    }
}
