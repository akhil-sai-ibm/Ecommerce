using PaymentService.Application.Interfaces.Clients;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace PaymentService.Infrastructure.Clients
{
    public class OrderServiceClient : IOrderServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public OrderServiceClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
        }

        private void ForwardAuthHeader()
        {
            var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"];
            if (authHeader.HasValue)
            {
                _httpClient.DefaultRequestHeaders.Authorization = System.Net.Http.Headers.AuthenticationHeaderValue.Parse(authHeader.Value.ToString());
            }
        }

        public async Task<bool> VerifyOrderOwnershipAsync(Guid orderId, Guid userId)
        {
            ForwardAuthHeader();
            var response = await _httpClient.GetAsync($"/api/order/{orderId}");
            if (!response.IsSuccessStatusCode)
                return false;
            
            var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
            return order != null && order.UserId == userId;
        }

        public async Task<int?> GetOrderStatusAsync(Guid orderId)
        {
            ForwardAuthHeader();
            var response = await _httpClient.GetAsync($"/api/order/{orderId}");
            if (!response.IsSuccessStatusCode)
                return null;
            
            var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
            return order?.Status;
        }

        public async Task<bool> MarkPaymentSuccessfulAsync(Guid orderId)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"/api/order/internal/{orderId}/payment-success");
            request.Headers.Add("X-Internal-Service-Key", "internal-secure-key-123");
            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        private class OrderResponse
        {
            public Guid Id { get; set; }
            public Guid UserId { get; set; }
            public int Status { get; set; }
        }
    }
}
