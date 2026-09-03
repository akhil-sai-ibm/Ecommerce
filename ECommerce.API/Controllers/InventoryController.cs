using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Application.Interfaces.Services;
using ECommerce.Application.DTOs.Inventory;

namespace ECommerce.API.Controllers
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
    }
}
