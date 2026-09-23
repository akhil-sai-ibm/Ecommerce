using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ECommerce.API;
using ECommerce.Application.DTOs.Auth;
using ECommerce.Application.DTOs.Products;
using ECommerce.Application.DTOs.Inventory;
using ECommerce.Infrastructure.Data;
using ECommerce.Domain.Entities;
using Xunit;
using System.Linq;
using ClosedXML.Excel;

namespace ECommerce.IntegrationTests
{
    public class BulkUploadIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private static readonly Guid AdminRoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        public BulkUploadIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        private RegisterRequest CreateUniqueRegisterRequest()
        {
            var uniqueSuffix = Guid.NewGuid().ToString("N");
            return new RegisterRequest
            {
                FirstName = "Test",
                LastName = "User",
                Email = $"testuser_{uniqueSuffix}@example.com",
                Password = "Password123!"
            };
        }

        private async Task<string> GetCustomerTokenAsync()
        {
            var req = CreateUniqueRegisterRequest();
            await _client.PostAsJsonAsync("/api/auth/register", req);
            var loginResp = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = req.Email, Password = req.Password });
            var data = await loginResp.Content.ReadFromJsonAsync<AuthResponse>();
            return data!.Token;
        }

        private async Task<string> GetAdminTokenAsync()
        {
            var req = CreateUniqueRegisterRequest();
            var regResp = await _client.PostAsJsonAsync("/api/auth/register", req);
            var regData = await regResp.Content.ReadFromJsonAsync<AuthResponse>();

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();
                var user = await db.Users.FindAsync(regData!.User.Id);
                user!.RoleId = AdminRoleId;
                await db.SaveChangesAsync();
            }

            var loginResp = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = req.Email, Password = req.Password });
            var data = await loginResp.Content.ReadFromJsonAsync<AuthResponse>();
            return data!.Token;
        }

        private async Task<Category> GetOrCreateCategoryAsync()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();
                var cat = db.Categories.FirstOrDefault(c => c.Name == "Integration Test Category");
                if (cat == null)
                {
                    cat = new Category { Id = Guid.NewGuid(), Name = "Integration Test Category", Description = "Test" };
                    db.Categories.Add(cat);
                    await db.SaveChangesAsync();
                }
                return cat;
            }
        }

        private byte[] GenerateExcelFile(List<Dictionary<string, object>> rows)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Products");

            if (rows.Count > 0)
            {
                var headers = rows[0].Keys.ToList();
                for (int i = 0; i < headers.Count; i++)
                {
                    worksheet.Cell(1, i + 1).Value = headers[i];
                }

                for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
                {
                    for (int colIndex = 0; colIndex < headers.Count; colIndex++)
                    {
                        var val = rows[rowIndex][headers[colIndex]];
                        worksheet.Cell(rowIndex + 2, colIndex + 1).Value = val?.ToString() ?? string.Empty;
                    }
                }
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private async Task<HttpResponseMessage> UploadExcelAsync(byte[] fileBytes, string fileName, string token)
        {
            var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            content.Add(fileContent, "file", fileName);

            var request = new HttpRequestMessage(HttpMethod.Post, "/api/products/bulk-upload")
            {
                Content = content
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return await _client.SendAsync(request);
        }

        [Fact]
        public async Task ValidExcelUpload_CreatesProductsAndInventory()
        {
            var token = await GetAdminTokenAsync();
            var category = await GetOrCreateCategoryAsync();
            
            var sku1 = $"SKU-VALID-1-{Guid.NewGuid():N}";
            var sku2 = $"SKU-VALID-2-{Guid.NewGuid():N}";

            var rows = new List<Dictionary<string, object>>
            {
                new() { { "Name", "P1" }, { "Description", "Desc1" }, { "Price", 10.5 }, { "SKU", sku1 }, { "Category", category.Name }, { "Status", "Active" }, { "Quantity", 50 }, { "ReorderLevel", 10 } },
                new() { { "Name", "P2" }, { "Description", "Desc2" }, { "Price", 20.0 }, { "SKU", sku2 }, { "Category", category.Name }, { "Status", "Inactive" }, { "Quantity", 0 }, { "ReorderLevel", 5 } }
            };

            var fileBytes = GenerateExcelFile(rows);
            var response = await UploadExcelAsync(fileBytes, "valid.xlsx", token);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<BulkProductUploadResultDto>();
            
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalRows);
            Assert.Equal(2, result.SuccessfulRows);
            Assert.Equal(0, result.FailedRows);
            Assert.Empty(result.Errors);
            Assert.Equal(1, result.BatchesProcessed);

            // Verify in DB via search endpoint
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var search1 = await _client.GetAsync($"/api/products/search?q={sku1}");
            var products1 = await search1.Content.ReadFromJsonAsync<List<ProductDto>>();
            Assert.Single(products1!);
            Assert.Equal(sku1, products1[0].SKU);

            // Verify Inventory
            var inv1 = await _client.GetAsync($"/api/inventory/{products1[0].Id}");
            var inventory1 = await inv1.Content.ReadFromJsonAsync<InventoryDto>();
            Assert.Equal(50, inventory1!.Quantity);
            Assert.Equal(10, inventory1.ReorderLevel);
        }

        [Fact]
        public async Task MultiBatchUpload_ProcessesSuccessfully()
        {
            var token = await GetAdminTokenAsync();
            var category = await GetOrCreateCategoryAsync();
            
            var rows = new List<Dictionary<string, object>>();
            // Batch size is configured to 100 in BulkProductOptions, so 105 rows should trigger 2 batches.
            for (int i = 0; i < 105; i++)
            {
                rows.Add(new Dictionary<string, object>
                {
                    { "Name", $"Batch Product {i}" },
                    { "Description", "" },
                    { "Price", 5.0 },
                    { "SKU", $"SKU-BATCH-{i}-{Guid.NewGuid():N}" },
                    { "Category", category.Name },
                    { "Status", "Active" },
                    { "Quantity", 10 },
                    { "ReorderLevel", 2 }
                });
            }

            var fileBytes = GenerateExcelFile(rows);
            var response = await UploadExcelAsync(fileBytes, "batch.xlsx", token);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<BulkProductUploadResultDto>();
            
            Assert.NotNull(result);
            Assert.Equal(105, result.TotalRows);
            Assert.Equal(105, result.SuccessfulRows);
            Assert.Equal(0, result.FailedRows);
            Assert.Equal(2, result.BatchesProcessed);
        }

        [Fact]
        public async Task PartialSuccess_InvalidRowsReportedCorrectly()
        {
            var token = await GetAdminTokenAsync();
            var category = await GetOrCreateCategoryAsync();
            
            var skuValid = $"SKU-PARTIAL-VALID-{Guid.NewGuid():N}";
            
            var rows = new List<Dictionary<string, object>>
            {
                new() { { "Name", "Valid Prod" }, { "Price", 10 }, { "SKU", skuValid }, { "Category", category.Name } }, // Row 2
                new() { { "Name", "" }, { "Price", 10 }, { "SKU", "SKU-INV-1" }, { "Category", category.Name } }, // Row 3 (Missing Name)
                new() { { "Name", "Inv Prod 2" }, { "Price", -5 }, { "SKU", "SKU-INV-2" }, { "Category", category.Name } }, // Row 4 (Negative Price)
                new() { { "Name", "Inv Prod 3" }, { "Price", 10 }, { "SKU", "SKU-INV-3" }, { "Category", "NonExistentCat999" } } // Row 5 (Bad Category)
            };

            var fileBytes = GenerateExcelFile(rows);
            var response = await UploadExcelAsync(fileBytes, "partial.xlsx", token);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<BulkProductUploadResultDto>();
            
            Assert.NotNull(result);
            Assert.Equal(4, result.TotalRows);
            Assert.Equal(1, result.SuccessfulRows);
            Assert.Equal(3, result.FailedRows);
            
            Assert.Contains(result.Errors, e => e.RowNumber == 3 && e.Error.Contains("name is required", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(result.Errors, e => e.RowNumber == 4 && e.Error.Contains("Price must be greater than zero", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(result.Errors, e => e.RowNumber == 5 && e.Error.Contains("Category 'NonExistentCat999' not found", StringComparison.OrdinalIgnoreCase));

            // Verify valid row is in DB
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var search = await _client.GetAsync($"/api/products/search?q={skuValid}");
            var products = await search.Content.ReadFromJsonAsync<List<ProductDto>>();
            Assert.Single(products!);
            Assert.Equal(skuValid, products[0].SKU);
        }

        [Fact]
        public async Task DuplicateSkuHandling_RejectsDuplicates()
        {
            var token = await GetAdminTokenAsync();
            var category = await GetOrCreateCategoryAsync();
            var sharedSku = $"SKU-DUP-{Guid.NewGuid():N}";
            
            var rows = new List<Dictionary<string, object>>
            {
                new() { { "Name", "Prod 1" }, { "Price", 10 }, { "SKU", sharedSku }, { "Category", category.Name } }, // Valid
                new() { { "Name", "Prod 2" }, { "Price", 20 }, { "SKU", sharedSku }, { "Category", category.Name } }, // Duplicate within file
            };

            var fileBytes = GenerateExcelFile(rows);
            var response = await UploadExcelAsync(fileBytes, "dupes.xlsx", token);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<BulkProductUploadResultDto>();
            
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalRows);
            Assert.Equal(1, result.SuccessfulRows);
            Assert.Equal(1, result.FailedRows);
            Assert.Contains(result.Errors, e => e.Error.Contains("Duplicate SKU"));

            // Test duplicate against existing DB SKU
            var newRows = new List<Dictionary<string, object>>
            {
                new() { { "Name", "Prod 3" }, { "Price", 30 }, { "SKU", sharedSku }, { "Category", category.Name } } // Duplicate against DB
            };
            
            var fileBytes2 = GenerateExcelFile(newRows);
            var response2 = await UploadExcelAsync(fileBytes2, "dupes_db.xlsx", token);

            Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
            var result2 = await response2.Content.ReadFromJsonAsync<BulkProductUploadResultDto>();
            
            Assert.NotNull(result2);
            Assert.Equal(1, result2.TotalRows);
            Assert.Equal(0, result2.SuccessfulRows);
            Assert.Equal(1, result2.FailedRows);
            Assert.Contains(result2.Errors, e => e.Error.Contains("SKU already exists"));
        }

        [Fact]
        public async Task CustomerAttemptingUpload_ReturnsForbidden()
        {
            var token = await GetCustomerTokenAsync();
            var rows = new List<Dictionary<string, object>> { new() { { "Name", "Test" } } };
            var fileBytes = GenerateExcelFile(rows);
            var response = await UploadExcelAsync(fileBytes, "forbidden.xlsx", token);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
