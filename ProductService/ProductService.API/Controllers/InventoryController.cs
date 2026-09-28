using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ProductService.Application.Interfaces.Services;
using ProductService.Application.DTOs.Inventory;

namespace ProductService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryService _inventoryService;

        public InventoryController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        [HttpGet("{productId}")]
        public async Task<IActionResult> GetByProductId(Guid productId)
        {
            var item = await _inventoryService.GetByProductIdAsync(productId).ConfigureAwait(false);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _inventoryService.GetAllAsync().ConfigureAwait(false);
            return Ok(items);
        }

        [HttpGet("low")]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetLowStock()
        {
            var items = await _inventoryService.GetLowStockItemsAsync().ConfigureAwait(false);
            return Ok(items);
        }

        [HttpPut("{productId}")]
        public async Task<IActionResult> Update(Guid productId, [FromBody] UpdateInventoryDto dto)
        {
            var inv = await _inventoryService.GetByProductIdAsync(productId).ConfigureAwait(false);
            if (inv == null) return NotFound();
            await _inventoryService.UpdateAsync(inv.Id, dto).ConfigureAwait(false);
            return NoContent();
        }

        [HttpPost("{productId}/increase")]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> Increase(Guid productId, [FromQuery] int amount)
        {
            await _inventoryService.IncreaseStockAsync(productId, amount).ConfigureAwait(false);
            return NoContent();
        }

        [HttpPost("{productId}/decrease")]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> Decrease(Guid productId, [FromQuery] int amount)
        {
            await _inventoryService.DecreaseStockAsync(productId, amount).ConfigureAwait(false);
            return NoContent();
        }

        [HttpPost("reserve")]
        [ProductService.API.Attributes.InternalServiceAuthorize]
        public async Task<IActionResult> Reserve([FromBody] ReservationRequestDto request)
        {
            var result = await _inventoryService.ReserveInventoryAsync(request).ConfigureAwait(false);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpPost("reserve/{reservationId}/release")]
        [ProductService.API.Attributes.InternalServiceAuthorize]
        public async Task<IActionResult> Release(Guid reservationId)
        {
            await _inventoryService.ReleaseInventoryAsync(reservationId).ConfigureAwait(false);
            return NoContent();
        }

        [HttpPost("reserve/{reservationId}/confirm")]
        [ProductService.API.Attributes.InternalServiceAuthorize]
        public async Task<IActionResult> Confirm(Guid reservationId)
        {
            await _inventoryService.ConfirmInventoryAsync(reservationId).ConfigureAwait(false);
            return NoContent();
        }
    }
}

