using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Application.Interfaces.Services;
using ECommerce.Application.DTOs.Products;
using ECommerce.Domain.Enums;

namespace ECommerce.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _productService.GetAllAsync().ConfigureAwait(false);
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var item = await _productService.GetByIdAsync(id).ConfigureAwait(false);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string? q, [FromQuery] Guid? categoryId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string? sortBy = null, [FromQuery] string? sortDirection = null)
        {
            var isAdmin = User.IsInRole("Admin");
            var status = isAdmin ? (ProductStatus?)null : ProductStatus.Active;

            var (items, total) = await _productService.SearchAsync(q, categoryId, pageNumber, pageSize, sortBy, sortDirection, status).ConfigureAwait(false);
            Response.Headers["X-Total-Count"] = total.ToString();
            return Ok(items);
        }

        [HttpGet("category/{categoryId}")]
        public async Task<IActionResult> GetByCategory(Guid categoryId)
        {
            var items = await _productService.GetByCategoryAsync(categoryId).ConfigureAwait(false);
            return Ok(items);
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
        {
            var created = await _productService.CreateAsync(dto).ConfigureAwait(false);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPost("bulk-upload")]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> BulkUpload(Microsoft.AspNetCore.Http.IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { Message = "No file uploaded." });

            var extension = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".xlsx")
                return BadRequest(new { Message = "Only .xlsx files are supported." });

            using var stream = file.OpenReadStream();
            var result = await _productService.BulkUploadAsync(stream).ConfigureAwait(false);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductDto dto)
        {
            await _productService.UpdateAsync(id, dto).ConfigureAwait(false);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _productService.DeleteAsync(id).ConfigureAwait(false);
            return Ok(result);
        }

        [HttpPost("{id}/image")]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> UploadImage(Guid id, Microsoft.AspNetCore.Http.IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { Message = "No file uploaded." });

            try
            {
                using var stream = file.OpenReadStream();
                var fileData = new ECommerce.Application.DTOs.Common.FileData
                {
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    Content = stream,
                    Length = file.Length
                };

                var newUrl = await _productService.UploadProductImageAsync(id, fileData).ConfigureAwait(false);
                return Ok(new { url = newUrl });
            }
            catch (ECommerce.Domain.Exceptions.DomainException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpDelete("{id}/image")]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteImage(Guid id)
        {
            try
            {
                await _productService.DeleteProductImageAsync(id).ConfigureAwait(false);
                return NoContent();
            }
            catch (ECommerce.Domain.Exceptions.DomainException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
