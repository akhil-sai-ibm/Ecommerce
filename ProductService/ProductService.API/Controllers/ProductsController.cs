using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductService.Application.DTOs.Common;
using ProductService.Application.DTOs.Products;
using ProductService.Application.Interfaces.Services;
using ProductService.Domain.Enums;
using ProductService.Domain.Exceptions;

namespace ProductService.API.Controllers
{
    /// <summary>
    /// Manages product catalog, search, pagination, bulk operations, and image attachments.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        /// <summary>
        /// Retrieves all products in the catalog.
        /// </summary>
        /// <returns>Collection of all products.</returns>
        /// <response code="200">Products retrieved successfully.</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var items = await _productService.GetAllAsync().ConfigureAwait(false);
            return Ok(items);
        }

        /// <summary>
        /// Retrieves a single product by its unique identifier.
        /// </summary>
        /// <param name="id">Product identifier.</param>
        /// <returns>Product details if found.</returns>
        /// <response code="200">Product found and returned.</response>
        /// <response code="404">Product not found.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var item = await _productService.GetByIdAsync(id).ConfigureAwait(false);
            if (item == null) return NotFound();
            return Ok(item);
        }

        /// <summary>
        /// Retrieves a batch of products by a list of product IDs.
        /// </summary>
        /// <param name="productIds">Collection of product identifiers.</param>
        /// <returns>Matching products.</returns>
        /// <response code="200">Batch products returned.</response>
        /// <response code="400">Invalid or malformed product identifiers list.</response>
        [HttpPost("bulk")]
        [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetBulkProducts([FromBody] IEnumerable<Guid> productIds)
        {
            var items = await _productService.GetBulkProductsAsync(productIds).ConfigureAwait(false);
            return Ok(items);
        }

        /// <summary>
        /// Searches and filters products with pagination and sorting. Returns total count in X-Total-Count header.
        /// </summary>
        /// <param name="q">Search query matching name or description.</param>
        /// <param name="categoryId">Optional category filter.</param>
        /// <param name="pageNumber">Page index (1-based, default 1).</param>
        /// <param name="pageSize">Page size (default 20).</param>
        /// <param name="sortBy">Sort column (name, price, etc.).</param>
        /// <param name="sortDirection">Sort direction (asc or desc).</param>
        /// <returns>Paged list of products matching the filter criteria.</returns>
        /// <response code="200">Products returned with X-Total-Count header.</response>
        [HttpGet("search")]
        [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Search([FromQuery] string? q, [FromQuery] Guid? categoryId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string? sortBy = null, [FromQuery] string? sortDirection = null)
        {
            var isAdmin = User.IsInRole("Admin");
            var status = isAdmin ? (ProductStatus?)null : ProductStatus.Active;

            var (items, total) = await _productService.SearchAsync(q, categoryId, pageNumber, pageSize, sortBy, sortDirection, status).ConfigureAwait(false);
            Response.Headers["X-Total-Count"] = total.ToString();
            return Ok(items);
        }

        /// <summary>
        /// Retrieves all products within a specific category.
        /// </summary>
        /// <param name="categoryId">Category identifier.</param>
        /// <returns>Collection of products belonging to the category.</returns>
        /// <response code="200">Category products returned.</response>
        [HttpGet("category/{categoryId}")]
        [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByCategory(Guid categoryId)
        {
            var items = await _productService.GetByCategoryAsync(categoryId).ConfigureAwait(false);
            return Ok(items);
        }

        /// <summary>
        /// Creates a new product in the catalog. Requires Admin role.
        /// </summary>
        /// <param name="dto">Product details including name, price, SKU, and category.</param>
        /// <returns>Created product with generated identifier.</returns>
        /// <response code="201">Product created successfully.</response>
        /// <response code="401">Missing or invalid authentication token.</response>
        /// <response code="403">User does not possess Admin role.</response>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
        {
            var created = await _productService.CreateAsync(dto).ConfigureAwait(false);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        /// <summary>
        /// Performs bulk product import via an Excel spreadsheet (.xlsx). Requires Admin role.
        /// </summary>
        /// <param name="file">Excel spreadsheet file containing product rows.</param>
        /// <returns>Summary of imported, failed, and processed rows.</returns>
        /// <response code="200">Bulk upload executed and report generated.</response>
        /// <response code="400">Invalid file format or missing upload.</response>
        /// <response code="401">Missing or invalid authentication token.</response>
        /// <response code="403">User does not possess Admin role.</response>
        /// <response code="415">Request payload is not multipart/form-data.</response>
        [HttpPost("bulk-upload")]
        [Authorize(Roles = "Admin")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(BulkProductUploadResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
        public async Task<IActionResult> BulkUpload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { Message = "No file uploaded." });

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".xlsx")
                return BadRequest(new { Message = "Only .xlsx files are supported." });

            using var stream = file.OpenReadStream();
            var result = await _productService.BulkUploadAsync(stream).ConfigureAwait(false);
            return Ok(result);
        }

        /// <summary>
        /// Updates an existing product's details. Requires Admin role.
        /// </summary>
        /// <param name="id">Product identifier.</param>
        /// <param name="dto">Updated product data.</param>
        /// <response code="204">Product updated successfully.</response>
        /// <response code="401">Missing or invalid authentication token.</response>
        /// <response code="403">User does not possess Admin role.</response>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductDto dto)
        {
            await _productService.UpdateAsync(id, dto).ConfigureAwait(false);
            return NoContent();
        }

        /// <summary>
        /// Deletes or soft-deletes a product by ID. Requires Admin role.
        /// </summary>
        /// <param name="id">Product identifier.</param>
        /// <returns>Result indicating whether product was permanently or soft deleted.</returns>
        /// <response code="200">Product deletion processed.</response>
        /// <response code="401">Missing or invalid authentication token.</response>
        /// <response code="403">User does not possess Admin role.</response>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ProductDeleteResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _productService.DeleteAsync(id).ConfigureAwait(false);
            return Ok(result);
        }

        /// <summary>
        /// Uploads an image for a specific product. Requires Admin role.
        /// </summary>
        /// <param name="id">Product identifier.</param>
        /// <param name="file">Image file (.jpg, .jpeg, .png).</param>
        /// <returns>Object with the updated image public URL.</returns>
        /// <response code="200">Product image uploaded successfully.</response>
        /// <response code="400">Invalid image file or format error.</response>
        /// <response code="401">Missing or invalid authentication token.</response>
        /// <response code="403">User does not possess Admin role.</response>
        /// <response code="415">Request payload is not multipart/form-data.</response>
        [HttpPost("{id}/image")]
        [Authorize(Roles = "Admin")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
        public async Task<IActionResult> UploadImage(Guid id, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { Message = "No file uploaded." });

            try
            {
                using var stream = file.OpenReadStream();
                var fileData = new FileData
                {
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    Content = stream,
                    Length = file.Length
                };

                var newUrl = await _productService.UploadProductImageAsync(id, fileData).ConfigureAwait(false);
                return Ok(new { url = newUrl });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Deletes the image associated with a product. Requires Admin role.
        /// </summary>
        /// <param name="id">Product identifier.</param>
        /// <response code="204">Image removed successfully.</response>
        /// <response code="400">Operation failure.</response>
        /// <response code="401">Missing or invalid authentication token.</response>
        /// <response code="403">User does not possess Admin role.</response>
        [HttpDelete("{id}/image")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteImage(Guid id)
        {
            try
            {
                await _productService.DeleteProductImageAsync(id).ConfigureAwait(false);
                return NoContent();
            }
            catch (DomainException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
