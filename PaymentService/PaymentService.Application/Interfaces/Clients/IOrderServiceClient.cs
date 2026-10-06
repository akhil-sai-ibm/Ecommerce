using System;
using System.Threading.Tasks;

namespace PaymentService.Application.Interfaces.Clients
{
    public interface IOrderServiceClient
    {
        Task<bool> VerifyOrderOwnershipAsync(Guid orderId, Guid userId);
        Task<bool> MarkPaymentSuccessfulAsync(Guid orderId);
        Task<int?> GetOrderStatusAsync(Guid orderId);
    }
}
