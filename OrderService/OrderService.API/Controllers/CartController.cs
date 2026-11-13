using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OrderService.Application.DTOs.Cart;
using OrderService.Application.Interfaces.Services;

namespace OrderService.API.Controllers
{
    /// <summary>
    /// Manages the shopping cart for authenticated users.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        /// <summary>
        /// Retrieves the current authenticated user's shopping cart.
        /// </summary>
        /// <returns>Cart details with current items and total calculations.</returns>
        /// <response code="200">Cart retrieved successfully.</response>
        /// <response code="401">Missing or invalid authentication token.</response>
        [HttpGet]
        [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetCart()
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            if (userId == Guid.Empty) return Unauthorized();

            var cart = await _cartService.GetCartAsync(userId);
            return Ok(cart);
        }

        /// <summary>
        /// Adds a product item to the user's shopping cart.
        /// </summary>
        /// <param name="addToCartDto">Product identifier and quantity to add.</param>
        /// <returns>Updated shopping cart.</returns>
        /// <response code="200">Item added to cart.</response>
        /// <response code="401">Missing or invalid authentication token.</response>
        [HttpPost("items")]
        [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartDto addToCartDto)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            if (userId == Guid.Empty) return Unauthorized();

            var cart = await _cartService.AddToCartAsync(userId, addToCartDto);
            return Ok(cart);
        }

        /// <summary>
        /// Updates the quantity of a specific item in the shopping cart.
        /// </summary>
        /// <param name="cartItemId">Cart item identifier.</param>
        /// <param name="dto">New quantity value (must be > 0).</param>
        /// <returns>Updated shopping cart.</returns>
        /// <response code="200">Cart item quantity updated.</response>
        /// <response code="400">Invalid quantity or business rule violation.</response>
        /// <response code="401">Missing or invalid authentication token.</response>
        /// <response code="404">Cart item not found.</response>
        [HttpPut("items/{cartItemId}")]
        [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateCartItemQuantity(Guid cartItemId, [FromBody] UpdateCartItemQuantityDto dto)
        {
            if (dto.Quantity <= 0) return BadRequest(new { message = "Quantity must be greater than zero." });

            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            if (userId == Guid.Empty) return Unauthorized();

            try
            {
                var cart = await _cartService.UpdateCartItemQuantityAsync(userId, cartItemId, dto.Quantity);
                return Ok(cart);
            }
            catch (System.Collections.Generic.KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Removes a specific item from the shopping cart.
        /// </summary>
        /// <param name="cartItemId">Cart item identifier to remove.</param>
        /// <returns>Updated shopping cart.</returns>
        /// <response code="200">Item removed from cart.</response>
        /// <response code="401">Missing or invalid authentication token.</response>
        [HttpDelete("items/{cartItemId}")]
        [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RemoveFromCart(Guid cartItemId)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            if (userId == Guid.Empty) return Unauthorized();

            var cart = await _cartService.RemoveFromCartAsync(userId, cartItemId);
            return Ok(cart);
        }
    }
}
