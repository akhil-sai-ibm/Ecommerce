using System;
using System.Threading.Tasks;
using OrderService.Application.DTOs.Cart;

namespace OrderService.Application.Interfaces.Services
{
    public interface ICartService
    {
        Task<CartDto> GetCartAsync(Guid userId);
        Task<CartDto> AddToCartAsync(Guid userId, AddToCartDto addToCartDto);
        Task<CartDto> UpdateCartItemQuantityAsync(Guid userId, Guid cartItemId, int quantity);
        Task<CartDto> RemoveFromCartAsync(Guid userId, Guid cartItemId);
        Task ClearCartAsync(Guid userId);
    }
}
