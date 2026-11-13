using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OrderService.API.Attributes;
using OrderService.Application.Interfaces.Services;

namespace OrderService.API.Controllers
{
    /// <summary>
    /// Internal endpoints for inter-service communication with PaymentService and other backend services.
    /// </summary>
    [ApiController]
    [Route("api/order/internal")]
    [InternalServiceAuthorize]
    public class OrderInternalController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderInternalController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        /// <summary>
        /// Marks an order as successfully paid upon receipt of payment confirmation from PaymentService. Internal endpoint.
        /// </summary>
        /// <param name="id">Order identifier.</param>
        /// <response code="200">Order successfully updated to Paid status.</response>
        /// <response code="400">Order cannot transition to Paid state.</response>
        /// <response code="401">Missing or invalid X-Internal-Service-Key.</response>
        [HttpPost("{id}/payment-success")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> MarkPaymentSuccessful(Guid id)
        {
            var internalKey = Request.Headers["X-Internal-Service-Key"].ToString();
            if (internalKey != "internal-secure-key-123")
            {
                return Unauthorized("Invalid internal key.");
            }

            try
            {
                await _orderService.HandlePaymentSuccessAsync(id);
                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
