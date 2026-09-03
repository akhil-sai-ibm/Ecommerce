using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Application.Interfaces.Services;
using ECommerce.Application.DTOs.Orders;

namespace ECommerce.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ECommerce.Application.Interfaces.Services.ICurrentUserService _currentUser;

        public OrdersController(IOrderService orderService, ECommerce.Application.Interfaces.Services.ICurrentUserService currentUser)
        {
            _orderService = orderService;
            _currentUser = currentUser;
        }

        [HttpGet("{id}")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> GetById(Guid id)
        {
            var order = await _orderService.GetByIdAsync(id).ConfigureAwait(false);
            if (order == null) return NotFound();

            var userId = _currentUser.UserId;
            var role = _currentUser.Role;
            if (role == "Admin" || (userId.HasValue && order.UserId == userId.Value))
            {
                return Ok(order);
            }

            return Forbid();
        }

        [HttpGet("user/{userId}")]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetByUserId(Guid userId)
        {
            var orders = await _orderService.GetByUserIdAsync(userId).ConfigureAwait(false);
            return Ok(orders);
        }

        [HttpGet("me")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> GetMyOrders()
        {
            var userId = _currentUser.UserId;
            if (!userId.HasValue) return Unauthorized();
            var orders = await _orderService.GetByUserIdAsync(userId.Value).ConfigureAwait(false);
            return Ok(orders);
        }

        [HttpPost("checkout")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> Checkout([FromHeader(Name = "Idempotency-Key")] string? idempotencyKey = null)
        {
            var userId = _currentUser.UserId;
            if (!userId.HasValue) return Unauthorized();

            try
            {
                var order = await _orderService.CreateOrderFromCartAsync(userId.Value, idempotencyKey).ConfigureAwait(false);
                return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
            }
            catch (Exception ex) when (ex.Message.Contains("idempotency"))
            {
                // Return 409 Conflict for idempotency-related errors
                return StatusCode(409, new { error = ex.Message });
            }
        }

        [HttpPut("{id}/status")]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromQuery] int status)
        {
            await _orderService.UpdateOrderStatusAsync(id, status).ConfigureAwait(false);
            return NoContent();
        }

        [HttpPost("{id}/cancel")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> Cancel(Guid id)
        {
            var userId = _currentUser.UserId;
            if (!userId.HasValue) return Unauthorized();

            // Get the order to verify ownership (customer can only cancel their own orders)
            var order = await _orderService.GetByIdAsync(id).ConfigureAwait(false);
            if (order == null) return NotFound();

            var role = _currentUser.Role;
            if (role != "Admin" && order.UserId != userId.Value)
            {
                return Forbid();
            }

            await _orderService.CancelOrderAsync(id).ConfigureAwait(false);
            return NoContent();
        }
    }
}
