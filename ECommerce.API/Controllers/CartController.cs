using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Application.Interfaces.Services;
using ECommerce.Application.DTOs.Cart;

namespace ECommerce.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;
        private readonly ICurrentUserService _currentUser;

        public CartController(ICartService cartService, ICurrentUserService currentUser)
        {
            _cartService = cartService;
            _currentUser = currentUser;
        }

        [HttpGet]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> GetForCurrentUser()
        {
            var userId = _currentUser.UserId;
            if (userId == null) return Unauthorized();
            var cart = await _cartService.GetByUserIdAsync(userId.Value).ConfigureAwait(false);
            if (cart == null) return NotFound();
            return Ok(cart);
        }

        [HttpPost("items")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> AddItem([FromBody] AddCartItemDto dto)
        {
            var userId = _currentUser.UserId;
            if (userId == null) return Unauthorized();
            await _cartService.AddItemAsync(userId.Value, dto).ConfigureAwait(false);
            return NoContent();
        }

        [HttpPut("items/{cartItemId}")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> UpdateItem(Guid cartItemId, [FromBody] UpdateCartItemDto dto)
        {
            var userId = _currentUser.UserId;
            if (userId == null) return Unauthorized();
            await _cartService.UpdateItemQuantityAsync(userId.Value, cartItemId, dto.Quantity).ConfigureAwait(false);
            return NoContent();
        }

        [HttpDelete("items/{cartItemId}")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> RemoveItem(Guid cartItemId)
        {
            var userId = _currentUser.UserId;
            if (userId == null) return Unauthorized();
            await _cartService.RemoveItemAsync(userId.Value, cartItemId).ConfigureAwait(false);
            return NoContent();
        }

        [HttpDelete]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> ClearCart()
        {
            var userId = _currentUser.UserId;
            if (userId == null) return Unauthorized();
            await _cartService.ClearCartAsync(userId.Value).ConfigureAwait(false);
            return NoContent();
        }
    }
}
