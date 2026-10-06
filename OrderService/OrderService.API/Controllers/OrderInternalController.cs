using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OrderService.Application.Interfaces.Services;

namespace OrderService.API.Controllers
{
    [ApiController]
    [Route("api/order/internal")]
    public class OrderInternalController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderInternalController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpPost("{id}/payment-success")]
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
