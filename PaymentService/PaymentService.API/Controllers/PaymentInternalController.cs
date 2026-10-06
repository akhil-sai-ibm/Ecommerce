using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Application.Interfaces.Services;

namespace PaymentService.API.Controllers
{
    [ApiController]
    [Route("api/internal/payments")]
    public class PaymentInternalController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentInternalController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpPost("{orderId}/refund-initiate")]
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

        [HttpGet("order/{orderId}")]
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
