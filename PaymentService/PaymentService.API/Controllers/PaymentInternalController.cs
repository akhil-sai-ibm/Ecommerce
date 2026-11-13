using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PaymentService.API.Attributes;
using PaymentService.Application.DTOs.Payments;
using PaymentService.Application.Interfaces.Services;

namespace PaymentService.API.Controllers
{
    /// <summary>
    /// Internal endpoints for inter-service communication with OrderService and background orchestrators.
    /// </summary>
    [ApiController]
    [Route("api/internal/payments")]
    [InternalServiceAuthorize]
    public class PaymentInternalController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentInternalController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        /// <summary>
        /// Initiates a compensating refund for an order (e.g., triggered when an order is cancelled or expires). Internal endpoint.
        /// </summary>
        /// <param name="orderId">Order identifier whose payment should be refunded.</param>
        /// <returns>Updated payment record with refund initiation status.</returns>
        /// <response code="200">Refund initiated successfully.</response>
        /// <response code="400">Order cannot be refunded or operation error.</response>
        /// <response code="401">Missing or invalid X-Internal-Service-Key.</response>
        [HttpPost("{orderId}/refund-initiate")]
        [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> InitiateRefund(Guid orderId)
        {
            var internalKey = Request.Headers["X-Internal-Service-Key"].ToString();
            if (internalKey != "internal-secure-key-123")
            {
                return Unauthorized("Invalid internal key.");
            }

            try
            {
                var paymentDto = await _paymentService.InitiateRefundAsync(orderId);
                return Ok(paymentDto);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves internal payment details by order identifier for inter-service validation. Internal endpoint.
        /// </summary>
        /// <param name="orderId">Order identifier.</param>
        /// <returns>Payment details if found.</returns>
        /// <response code="200">Payment found and returned.</response>
        /// <response code="400">Query error.</response>
        /// <response code="401">Missing or invalid X-Internal-Service-Key.</response>
        /// <response code="404">Payment record not found for the given order ID.</response>
        [HttpGet("order/{orderId}")]
        [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetPaymentByOrderId(Guid orderId)
        {
            var internalKey = Request.Headers["X-Internal-Service-Key"].ToString();
            if (internalKey != "internal-secure-key-123")
            {
                return Unauthorized("Invalid internal key.");
            }

            try
            {
                var paymentDto = await _paymentService.GetInternalOrderPaymentAsync(orderId);
                if (paymentDto == null) return NotFound();
                return Ok(paymentDto);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
