using System;
using System.Threading.Tasks;
using ECommerce.Application.DTOs.Cart;

namespace ECommerce.Application.Interfaces.Services
{
    public interface ICartService
    {
        Task<CartDto?> GetByUserIdAsync(Guid userId);
        Task<CartDto?> GetWithItemsAsync(Guid cartId);
        Task AddItemAsync(Guid userId, AddCartItemDto dto);
        Task UpdateItemQuantityAsync(Guid userId, Guid cartItemId, int quantity);
        Task RemoveItemAsync(Guid userId, Guid cartItemId);
        Task ClearCartAsync(Guid userId);
    }
}
