using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductService.Application.DTOs.Categories;
using ProductService.Application.Interfaces.Services;

namespace ProductService.API.Controllers
{
    /// <summary>
    /// Manages product categories.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        /// <summary>
        /// Retrieves all categories.
        /// </summary>
        /// <returns>Collection of all product categories.</returns>
        /// <response code="200">Categories retrieved successfully.</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<CategoryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var items = await _categoryService.GetAllAsync().ConfigureAwait(false);
            return Ok(items);
        }

        /// <summary>
        /// Retrieves a single category by its unique identifier.
        /// </summary>
        /// <param name="id">Category identifier.</param>
        /// <returns>Category details.</returns>
        /// <response code="200">Category found and returned.</response>
        /// <response code="404">Category not found.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var item = await _categoryService.GetByIdAsync(id).ConfigureAwait(false);
            if (item == null) return NotFound();
            return Ok(item);
        }

        /// <summary>
        /// Creates a new product category. Requires Admin role.
        /// </summary>
        /// <param name="dto">Category creation parameters.</param>
        /// <returns>Created category.</returns>
        /// <response code="201">Category created successfully.</response>
        /// <response code="401">Missing or invalid authentication token.</response>
        /// <response code="403">User does not possess Admin role.</response>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Create([FromBody] CreateCategoryDto dto)
        {
            var created = await _categoryService.CreateAsync(dto).ConfigureAwait(false);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        /// <summary>
        /// Updates an existing category by ID. Requires Admin role.
        /// </summary>
        /// <param name="id">Category identifier.</param>
        /// <param name="dto">Updated category data.</param>
        /// <response code="204">Category updated successfully.</response>
        /// <response code="401">Missing or invalid authentication token.</response>
        /// <response code="403">User does not possess Admin role.</response>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryDto dto)
        {
            await _categoryService.UpdateAsync(id, dto).ConfigureAwait(false);
            return NoContent();
        }

        /// <summary>
        /// Deletes a category by ID. Requires Admin role.
        /// </summary>
        /// <param name="id">Category identifier.</param>
        /// <response code="204">Category deleted successfully.</response>
        /// <response code="401">Missing or invalid authentication token.</response>
        /// <response code="403">User does not possess Admin role.</response>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _categoryService.DeleteAsync(id).ConfigureAwait(false);
            return NoContent();
        }
    }
}
