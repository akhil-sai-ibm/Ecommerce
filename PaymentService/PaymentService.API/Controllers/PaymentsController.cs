using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Application.DTOs.Payments;
using PaymentService.Application.Interfaces.Services;

namespace PaymentService.API.Controllers
{
    /// <summary>
    /// Manages customer payment processing, payment lookups, and administrative refund completions.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        /// <summary>
        /// Creates a payment transaction for an order with idempotency guarantee.
        /// </summary>
        /// <param name="request">Payment parameters including order identifier, amount, and payment method.</param>
        /// <param name="idempotencyKey">Unique idempotency key header to prevent duplicate charges.</param>
        /// <returns>Created payment transaction record.</returns>
        /// <response code="201">Payment record created successfully.</response>
        /// <response code="400">Missing Idempotency-Key header or invalid request data.</response>
        /// <response code="401">Missing or invalid authentication token.</response>
        /// <response code="409">Conflicting concurrent payment request detected for key.</response>
        [HttpPost]
        [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentDto request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
        {
            if (string.IsNullOrEmpty(idempotencyKey))
                return BadRequest("Idempotency-Key header is required.");

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            try
            {
                var result = await _paymentService.CreatePaymentAsync(userId, request, idempotencyKey, Request.Path);
                return CreatedAtAction(nameof(GetOrderPayment), new { orderId = request.OrderId }, result);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("Previous request"))
                    return Conflict(new { error = ex.Message });
                
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Processes payment charge through the payment gateway for a created payment.
        /// </summary>
        /// <param name="id">Payment transaction identifier.</param>
        /// <returns>Processed payment record.</returns>
        /// <response code="200">Payment processed successfully.</response>
        /// <response code="400">Payment failed or cannot be processed.</response>
        /// <response code="401">Missing or invalid authentication token.</response>
        [HttpPost("{id}/process")]
        [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ProcessPayment(Guid id)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            try
            {
                var result = await _paymentService.ProcessPaymentAsync(userId, id);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves the payment record associated with a specific order.
        /// </summary>
        /// <param name="orderId">Order identifier.</param>
        /// <returns>Payment details if exists.</returns>
        /// <response code="200">Payment found and returned.</response>
        /// <response code="204">No payment record found for this order.</response>
        /// <response code="400">Error retrieving payment.</response>
        /// <response code="401">Missing or invalid authentication token.</response>
        [HttpGet("order/{orderId}")]
        [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetOrderPayment(Guid orderId)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            try
            {
                var result = await _paymentService.GetOrderPaymentAsync(userId, orderId);
                if (result == null)
                    return NoContent();

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Completes a refund transaction for a payment. Requires Admin role.
        /// </summary>
        /// <param name="id">Payment identifier to refund.</param>
        /// <param name="idempotencyKey">Unique idempotency key header to prevent duplicate refund processing.</param>
        /// <returns>Updated payment record with Refunded status.</returns>
        /// <response code="200">Refund completed successfully.</response>
        /// <response code="400">Missing Idempotency-Key header or invalid refund state.</response>
        /// <response code="401">Missing or invalid authentication token.</response>
        /// <response code="403">User does not possess Admin role.</response>
        /// <response code="409">Conflicting concurrent refund request detected.</response>
        [Authorize(Roles = "Admin")]
        [HttpPost("{id}/refund-complete")]
        [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CompleteRefund(Guid id, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
        {
            if (string.IsNullOrEmpty(idempotencyKey))
                return BadRequest("Idempotency-Key header is required.");

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            try
            {
                var result = await _paymentService.CompleteRefundAsync(userId, id, idempotencyKey, Request.Path);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("Previous request"))
                    return Conflict(new { error = ex.Message });
                
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
