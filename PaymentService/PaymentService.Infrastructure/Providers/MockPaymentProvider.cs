using PaymentService.Application.Interfaces.Providers;
using System;
using System.Threading.Tasks;

namespace PaymentService.Infrastructure.Providers
{
    public class MockPaymentProvider : IPaymentProvider
    {
        public Task<PaymentProviderResult> ProcessPaymentAsync(decimal amount, string currency = "USD")
        {
            // Existing mock logic: Amount <= 1,000,000 succeeds
            bool isSuccess = amount <= 1000000;

            var result = new PaymentProviderResult
            {
                IsSuccess = isSuccess,
                TransactionId = isSuccess ? $"txn_{Guid.NewGuid():N}" : string.Empty,
                ErrorMessage = isSuccess ? null : "Payment failed due to mock validation (amount > 1,000,000)."
            };

            return Task.FromResult(result);
        }

        public Task<PaymentProviderResult> RefundPaymentAsync(string transactionId, decimal amount, string currency = "USD")
        {
            // Mock refund succeeds if transaction ID is not empty
            bool isSuccess = !string.IsNullOrEmpty(transactionId);
            var result = new PaymentProviderResult
            {
                IsSuccess = isSuccess,
                TransactionId = isSuccess ? $"ref_{Guid.NewGuid():N}" : string.Empty,
                ErrorMessage = isSuccess ? null : "Refund failed: invalid transaction ID."
            };

            return Task.FromResult(result);
        }
    }
}
