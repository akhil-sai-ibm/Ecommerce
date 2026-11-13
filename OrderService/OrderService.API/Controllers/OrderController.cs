using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OrderService.Application.DTOs.Order;
using OrderService.Application.Interfaces.Services;

namespace OrderService.API.Controllers
{
    /// <summary>
    /// Manages customer order lifecycle, checkout creation, order tracking, status updates, and cancellations.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        /// <summary>
        /// Creates a new customer order from checkout with idempotency protection.
        /// </summary>
        /// <param name="createOrderDto">Order checkout details including shipping address and items.</param>
        /// <param name="idempotencyKey">Unique idempotency key header to prevent duplicate orders.</param>
        /// <returns>Created order details.</returns>
        /// <response code="201">Order created successfully.</response>
        /// <response code="400">Missing Idempotency-Key header or invalid order data.</response>
        /// <response code="401">Missing or invalid authentication token.</response>
        [HttpPost]
        [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto createOrderDto, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
        {
            if (string.IsNullOrEmpty(idempotencyKey))
            {
                return BadRequest("Idempotency-Key header is required.");
            }

            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            if (userId == Guid.Empty) return Unauthorized();

            try
            {
                var order = await _orderService.CreateOrderAsync(userId, createOrderDto, idempotencyKey, Request.Path);
                // UC3 idempotency rule: return 201 with the order, never 409
                return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves order details by order identifier. Customers may view only their own orders; Admins may view any order.
        /// </summary>
        /// <param name="id">Order identifier.</param>
        /// <returns>Order details with items.</returns>
        /// <response code="200">Order found and returned.</response>
        /// <response code="401">Missing or invalid authentication token.</response>
        /// <response code="404">Order not found or access denied.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOrder(Guid id)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId) || userId == Guid.Empty) 
                return Unauthorized();

            try
            {
                if (User.IsInRole("Admin"))
                {
                    var order = await _orderService.GetOrderForAdminAsync(id);
                    return Ok(order);
                }
                else
                {
                    var order = await _orderService.GetOrderAsync(id, userId);
                    return Ok(order);
                }
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
        }

        /// <summary>
        /// Retrieves all orders belonging to the authenticated customer.
        /// </summary>
        /// <returns>Collection of customer orders.</returns>
        /// <response code="200">Orders retrieved successfully.</response>
        /// <response code="401">Missing or invalid authentication token.</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<OrderDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetUserOrders()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId) || userId == Guid.Empty) 
                return Unauthorized();

            var orders = await _orderService.GetUserOrdersAsync(userId);
            return Ok(orders);
        }

        /// <summary>
        /// Retrieves all orders for a specific user ID. Requires Admin role.
        /// </summary>
        /// <param name="userId">Target user identifier.</param>
        /// <returns>Collection of user orders.</returns>
        /// <response code="200">Orders retrieved successfully.</response>
        /// <response code="401">Missing or invalid authentication token.</response>
        /// <response code="403">User does not possess Admin role.</response>
        [Authorize(Roles = "Admin")]
        [HttpGet("user/{userId}")]
        [ProducesResponseType(typeof(IEnumerable<OrderDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetUserOrdersForAdmin(Guid userId)
        {
            var orders = await _orderService.GetUserOrdersAsync(userId);
            return Ok(orders);
        }

        /// <summary>
        /// Updates the status of an order. Requires Admin role.
        /// </summary>
        /// <param name="id">Order identifier.</param>
        /// <param name="status">New order status numeric value (0: Pending, 1: Confirmed, 2: Processing, 3: Shipped, 4: Delivered, 5: Cancelled).</param>
        /// <returns>Updated order details.</returns>
        /// <response code="200">Order status updated successfully.</response>
        /// <response code="400">Invalid status transition.</response>
        /// <response code="401">Missing or invalid authentication token.</response>
        /// <response code="403">User does not possess Admin role.</response>
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}/status")]
        [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromQuery] int status)
        {
            try
            {
                var order = await _orderService.UpdateOrderStatusAsync(id, status);
                return Ok(order);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Cancels a pending order and initiates compensating refund and inventory release.
        /// </summary>
        /// <param name="id">Order identifier.</param>
        /// <response code="200">Order cancelled successfully.</response>
        /// <response code="400">Order cannot be cancelled in its current state.</response>
        /// <response code="401">Missing or invalid authentication token.</response>
        [HttpPost("{id}/cancel")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CancelOrder(Guid id)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId) || userId == Guid.Empty) 
                return Unauthorized();

            try
            {
                await _orderService.CancelOrderAsync(id, userId);
                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
