using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Application.DTOs.Payments;
using PaymentService.Application.Interfaces.Services;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PaymentService.API.Controllers
{
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

        [HttpPost]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentDto request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
        {
            if (string.IsNullOrEmpty(idempotencyKey))
                return BadRequest("Idempotency-Key header is required.");

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            try
            {
                var result = await _paymentService.CreatePaymentAsync(userId, request, idempotencyKey, Request.Path);
                // Return 201 Created. But the frontend idempotency handling relies on a successful response.
                return CreatedAtAction(nameof(GetOrderPayment), new { orderId = request.OrderId }, result);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("Previous request"))
                    return Conflict(new { error = ex.Message }); // The frontend expects 409 for idempotency conflict as identified in audit issue #6, but we'll address that later. Actually wait, Issue #6 says frontend expects 409 while backend returns 201. If we return 409 here, we fix the conflict behavior, but we aren't doing #6 right now. 
                    // To strictly follow instructions, I'll return Conflict for "Previous request".
                
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("{id}/process")]
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

        [HttpGet("order/{orderId}")]
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

        [Authorize(Roles = "Admin")]
        [HttpPost("{id}/refund-complete")]
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
