using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OrderService.Application.Interfaces.Clients;

namespace OrderService.Infrastructure.Clients
{
    public class PaymentServiceClient : IPaymentServiceClient
    {
        private readonly HttpClient _httpClient;

        public PaymentServiceClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            var paymentServiceUrl = configuration["PaymentServiceUrl"] ?? "https://localhost:7216/";
            _httpClient.BaseAddress = new Uri(paymentServiceUrl);
        }

        public async Task InitiateRefundAsync(Guid orderId)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"api/internal/payments/{orderId}/refund-initiate");
            request.Headers.Add("X-Internal-Service-Key", "internal-secure-key-123");
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }

        public async Task<OrderService.Application.DTOs.Payments.PaymentDto?> GetPaymentByOrderIdAsync(Guid orderId)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"api/internal/payments/order/{orderId}");
            request.Headers.Add("X-Internal-Service-Key", "internal-secure-key-123");
            var response = await _httpClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return System.Text.Json.JsonSerializer.Deserialize<OrderService.Application.DTOs.Payments.PaymentDto>(
                    content,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );
            }
            return null;
        }
    }
}
