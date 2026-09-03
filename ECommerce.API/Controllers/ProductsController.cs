using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Application.Interfaces.Services;
using ECommerce.Application.DTOs.Products;

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
            var (items, total) = await _productService.SearchAsync(q, categoryId, pageNumber, pageSize, sortBy, sortDirection).ConfigureAwait(false);
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
            await _productService.DeleteAsync(id).ConfigureAwait(false);
            return NoContent();
        }
    }
}
