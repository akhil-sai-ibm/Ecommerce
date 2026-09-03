using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Application.Interfaces.Services;
using ECommerce.Application.DTOs.Payments;
using ECommerce.Domain.Exceptions;

namespace ECommerce.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IOrderService _orderService;
        private readonly ICurrentUserService _currentUser;

        public PaymentsController(
            IPaymentService paymentService, 
            IOrderService orderService,
            ICurrentUserService currentUser)
        {
            _paymentService = paymentService;
            _orderService = orderService;
            _currentUser = currentUser;
        }

        /// <summary>
        /// Create a payment for an order
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentDto dto)
        {
            try
            {
                // Verify order exists and user owns it
                var order = await _orderService.GetByIdAsync(dto.OrderId).ConfigureAwait(false);
                if (order == null)
                    return NotFound(new { message = "Order not found" });

                var userId = _currentUser.UserId;
                var role = _currentUser.Role;

                // Customer can only create payments for their own orders
                // Admin can create payments for any order
                if (role != "Admin" && (!userId.HasValue || order.UserId != userId.Value))
                    return Forbid();

                var payment = await _paymentService.CreateAsync(dto).ConfigureAwait(false);
                return CreatedAtAction(nameof(GetPayment), new { id = payment.Id }, payment);
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get payment by ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetPayment(Guid id)
        {
            var payment = await _paymentService.GetByIdAsync(id).ConfigureAwait(false);
            if (payment == null)
                return NotFound(new { message = "Payment not found" });

            // Verify user has access to this payment
            var order = await _orderService.GetByIdAsync(payment.OrderId).ConfigureAwait(false);
            if (order == null)
                return NotFound(new { message = "Associated order not found" });

            var userId = _currentUser.UserId;
            var role = _currentUser.Role;

            if (role != "Admin" && (!userId.HasValue || order.UserId != userId.Value))
                return Forbid();

            return Ok(payment);
        }

        /// <summary>
        /// Get payment by order ID
        /// </summary>
        [HttpGet("order/{orderId}")]
        [Authorize]
        public async Task<IActionResult> GetPaymentByOrder(Guid orderId)
        {
            var order = await _orderService.GetByIdAsync(orderId).ConfigureAwait(false);
            if (order == null)
                return NotFound(new { message = "Order not found" });

            var userId = _currentUser.UserId;
            var role = _currentUser.Role;

            if (role != "Admin" && (!userId.HasValue || order.UserId != userId.Value))
                return Forbid();

            var payment = await _paymentService.GetByOrderIdAsync(orderId).ConfigureAwait(false);
            if (payment == null)
                return NotFound(new { message = "Payment not found for this order" });

            return Ok(payment);
        }

        /// <summary>
        /// Process a pending payment (mock payment processor)
        /// </summary>
        [HttpPost("{id}/process")]
        [Authorize]
        public async Task<IActionResult> ProcessPayment(Guid id)
        {
            try
            {
                var payment = await _paymentService.GetByIdAsync(id).ConfigureAwait(false);
                if (payment == null)
                    return NotFound(new { message = "Payment not found" });

                // Verify user has access
                var order = await _orderService.GetByIdAsync(payment.OrderId).ConfigureAwait(false);
                if (order == null)
                    return NotFound(new { message = "Associated order not found" });

                var userId = _currentUser.UserId;
                var role = _currentUser.Role;

                if (role != "Admin" && (!userId.HasValue || order.UserId != userId.Value))
                    return Forbid();

                // Process the payment
                var processedPayment = await _paymentService.ProcessPaymentAsync(id).ConfigureAwait(false);
                return Ok(processedPayment);
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Update payment status (Admin only)
        /// </summary>
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdatePaymentStatus(Guid id, [FromBody] UpdatePaymentStatusRequest request)
        {
            try
            {
                await _paymentService.UpdateStatusAsync(id, request.Status).ConfigureAwait(false);
                var payment = await _paymentService.GetByIdAsync(id).ConfigureAwait(false);
                return Ok(payment);
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    /// <summary>
    /// Request model for updating payment status
    /// </summary>
    public class UpdatePaymentStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }
}
