using System.Threading.Tasks;

namespace PaymentService.Application.Interfaces.Providers
{
    public class PaymentProviderResult
    {
        public bool IsSuccess { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
    }

    public interface IPaymentProvider
    {
        Task<PaymentProviderResult> ProcessPaymentAsync(decimal amount, string currency = "USD");
        Task<PaymentProviderResult> RefundPaymentAsync(string transactionId, decimal amount, string currency = "USD");
    }
}
