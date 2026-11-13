using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductService.API.Attributes;
using ProductService.Application.DTOs.Inventory;
using ProductService.Application.Interfaces.Services;

namespace ProductService.API.Controllers
{
    /// <summary>
    /// Manages product inventory stock levels, low-stock alerts, and inter-service inventory reservations.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryService _inventoryService;

        public InventoryController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        /// <summary>
        /// Retrieves the current inventory record for a specific product.
        /// </summary>
        /// <param name="productId">Product identifier.</param>
        /// <returns>Inventory details including stock quantity and reorder level.</returns>
        /// <response code="200">Inventory record found and returned.</response>
        /// <response code="404">Inventory record not found.</response>
        [HttpGet("{productId}")]
        [ProducesResponseType(typeof(InventoryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByProductId(Guid productId)
        {
            var item = await _inventoryService.GetByProductIdAsync(productId).ConfigureAwait(false);
            if (item == null) return NotFound();
            return Ok(item);
        }

        /// <summary>
        /// Retrieves all inventory records across the catalog.
        /// </summary>
        /// <returns>Collection of all inventory records.</returns>
        /// <response code="200">Inventory records retrieved successfully.</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<InventoryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var items = await _inventoryService.GetAllAsync().ConfigureAwait(false);
            return Ok(items);
        }

        /// <summary>
        /// Retrieves products whose inventory is at or below the reorder threshold. Requires Admin role.
        /// </summary>
        /// <returns>Collection of low-stock inventory records.</returns>
        /// <response code="200">Low-stock records returned.</response>
        /// <response code="401">Missing or invalid authentication token.</response>
        /// <response code="403">User does not possess Admin role.</response>
        [HttpGet("low")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(IEnumerable<InventoryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetLowStock()
        {
            var items = await _inventoryService.GetLowStockItemsAsync().ConfigureAwait(false);
            return Ok(items);
        }

        /// <summary>
        /// Updates the inventory stock quantity and reorder level for a product. Requires Admin role.
        /// </summary>
        /// <param name="productId">Product identifier.</param>
        /// <param name="dto">Updated inventory values.</param>
        /// <response code="204">Inventory updated successfully.</response>
        /// <response code="401">Missing or invalid authentication token.</response>
        /// <response code="403">User does not possess Admin role.</response>
        /// <response code="404">Inventory record for product not found.</response>
        [HttpPut("{productId}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid productId, [FromBody] UpdateInventoryDto dto)
        {
            var inv = await _inventoryService.GetByProductIdAsync(productId).ConfigureAwait(false);
            if (inv == null) return NotFound();
            await _inventoryService.UpdateAsync(inv.Id, dto).ConfigureAwait(false);
            return NoContent();
        }

        /// <summary>
        /// Increases the stock level for a product by the specified amount. Requires Admin role.
        /// </summary>
        /// <param name="productId">Product identifier.</param>
        /// <param name="amount">Quantity to add to inventory.</param>
        /// <response code="204">Stock increased successfully.</response>
        /// <response code="401">Missing or invalid authentication token.</response>
        /// <response code="403">User does not possess Admin role.</response>
        [HttpPost("{productId}/increase")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Increase(Guid productId, [FromQuery] int amount)
        {
            await _inventoryService.IncreaseStockAsync(productId, amount).ConfigureAwait(false);
            return NoContent();
        }

        /// <summary>
        /// Decreases the stock level for a product by the specified amount. Requires Admin role.
        /// </summary>
        /// <param name="productId">Product identifier.</param>
        /// <param name="amount">Quantity to deduct from inventory.</param>
        /// <response code="204">Stock decreased successfully.</response>
        /// <response code="401">Missing or invalid authentication token.</response>
        /// <response code="403">User does not possess Admin role.</response>
        [HttpPost("{productId}/decrease")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Decrease(Guid productId, [FromQuery] int amount)
        {
            await _inventoryService.DecreaseStockAsync(productId, amount).ConfigureAwait(false);
            return NoContent();
        }

        /// <summary>
        /// Reserves inventory items for a checkout order. Internal service endpoint protected by X-Internal-Service-Key.
        /// </summary>
        /// <param name="request">Reservation request containing items, quantities, and expiration window.</param>
        /// <returns>Reservation result with success status and any missing items.</returns>
        /// <response code="200">Reservation succeeded.</response>
        /// <response code="400">Reservation failed due to insufficient stock.</response>
        /// <response code="401">Missing or invalid internal service key.</response>
        [HttpPost("reserve")]
        [InternalServiceAuthorize]
        [ProducesResponseType(typeof(ReservationResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ReservationResultDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Reserve([FromBody] ReservationRequestDto request)
        {
            var result = await _inventoryService.ReserveInventoryAsync(request).ConfigureAwait(false);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        /// <summary>
        /// Releases a previously held inventory reservation (e.g., when checkout expires or is cancelled). Internal endpoint.
        /// </summary>
        /// <param name="reservationId">Reservation identifier to release.</param>
        /// <response code="204">Reservation released successfully.</response>
        /// <response code="401">Missing or invalid internal service key.</response>
        [HttpPost("reserve/{reservationId}/release")]
        [InternalServiceAuthorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Release(Guid reservationId)
        {
            await _inventoryService.ReleaseInventoryAsync(reservationId).ConfigureAwait(false);
            return NoContent();
        }

        /// <summary>
        /// Confirms a held inventory reservation permanently (e.g., when payment succeeds). Internal endpoint.
        /// </summary>
        /// <param name="reservationId">Reservation identifier to confirm.</param>
        /// <response code="204">Reservation confirmed successfully.</response>
        /// <response code="401">Missing or invalid internal service key.</response>
        [HttpPost("reserve/{reservationId}/confirm")]
        [InternalServiceAuthorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Confirm(Guid reservationId)
        {
            await _inventoryService.ConfirmInventoryAsync(reservationId).ConfigureAwait(false);
            return NoContent();
        }
    }
}
