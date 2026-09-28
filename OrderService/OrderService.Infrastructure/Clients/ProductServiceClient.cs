using Microsoft.Extensions.Configuration;
using OrderService.Application.Interfaces.Clients;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace OrderService.Infrastructure.Clients
{
    public class ProductServiceClient : IProductServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _internalKey;

        public ProductServiceClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(configuration["ProductServiceUrl"] ?? "https://localhost:7131");
            _internalKey = configuration["InternalServiceKey"] ?? string.Empty;
        }

        public async Task ConfirmInventoryAsync(Guid reservationId)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"/api/inventory/reserve/{reservationId}/confirm");
            request.Headers.Add("X-Internal-Service-Key", _internalKey);
            
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }

        public async Task<IEnumerable<ProductSnapshot>> GetBulkProductsAsync(IEnumerable<Guid> productIds)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/products/bulk", productIds);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<ProductSnapshot>>() ?? Enumerable.Empty<ProductSnapshot>();
        }

        public async Task ReleaseInventoryAsync(Guid reservationId)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"/api/inventory/reserve/{reservationId}/release");
            request.Headers.Add("X-Internal-Service-Key", _internalKey);
            
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }

        public async Task<bool> ReserveInventoryAsync(Guid reservationId, IEnumerable<(Guid ProductId, int Quantity)> items)
        {
            var payload = new
            {
                ReservationId = reservationId,
                Items = items.Select(x => new { x.ProductId, x.Quantity })
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "/api/inventory/reserve")
            {
                Content = JsonContent.Create(payload)
            };
            request.Headers.Add("X-Internal-Service-Key", _internalKey);
            
            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                return false;
            }
            response.EnsureSuccessStatusCode();
            return false;
        }
    }
}
