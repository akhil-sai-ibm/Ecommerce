using System;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Application.DTOs.Cart;
using ECommerce.Application.Interfaces.Repositories;
using ECommerce.Application.Interfaces.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Exceptions;

namespace ECommerce.Application.Services
{
    public class CartService : ICartService
    {
        private readonly ICartRepository _cartRepository;
        private readonly IProductRepository _productRepository;
        private readonly IInventoryRepository _inventoryRepository;
        private readonly IUserRepository _userRepository;

        public CartService(ICartRepository cartRepository, IProductRepository productRepository, IInventoryRepository inventoryRepository, IUserRepository userRepository)
        {
            _cartRepository = cartRepository;
            _productRepository = productRepository;
            _inventoryRepository = inventoryRepository;
            _userRepository = userRepository;
        }

        public async Task<CartDto?> GetByUserIdAsync(Guid userId)
        {
            var cart = await _cartRepository.GetByUserIdAsync(userId).ConfigureAwait(false);
            if (cart == null) return null;
            return await BuildCartDtoAsync(cart).ConfigureAwait(false);
        }

        public async Task<CartDto?> GetWithItemsAsync(Guid cartId)
        {
            var cart = await _cartRepository.GetWithItemsAsync(cartId).ConfigureAwait(false);
            if (cart == null) return null;
            return await BuildCartDtoAsync(cart).ConfigureAwait(false);
        }

        public async Task AddItemAsync(Guid userId, AddCartItemDto dto)
        {
            if (dto.Quantity <= 0) throw new DomainException("Quantity must be greater than zero.");

            var user = await _userRepository.GetByIdAsync(userId).ConfigureAwait(false);
            if (user == null) throw new DomainException("User not found.");

            var product = await _productRepository.GetByIdAsync(dto.ProductId).ConfigureAwait(false);
            if (product == null) throw new DomainException("Product not found.");
            if (product.Status != Domain.Enums.ProductStatus.Active) throw new DomainException("Product is not available.");

            var inventory = await _inventoryRepository.GetByProductIdAsync(dto.ProductId).ConfigureAwait(false);
            if (inventory == null) throw new DomainException("Inventory record not found.");
            if (inventory.Quantity < dto.Quantity) throw new DomainException("Insufficient stock.");

            var cart = await _cartRepository.GetByUserIdAsync(userId).ConfigureAwait(false);
            if (cart == null)
            {
                cart = new Cart { Id = Guid.NewGuid(), UserId = userId, CreatedAt = DateTime.UtcNow };
                await _cartRepository.AddAsync(cart).ConfigureAwait(false);
                // Save the cart first to ensure it exists in the database before adding items
                await _cartRepository.SaveChangesAsync().ConfigureAwait(false);
                var savedCart = await _cartRepository.GetByUserIdAsync(userId)
    .ConfigureAwait(false);

                if (savedCart == null)
                    throw new DomainException("Cart was not persisted to the database.");

                cart = savedCart;
            }

            // check existing cart item
            var existingItem = cart.Items?.FirstOrDefault(i => i.ProductId == dto.ProductId);
            if (existingItem != null)
            {
                existingItem.Quantity += dto.Quantity;
                _cartRepository.Update(cart);
            }
            else
            {
                var item = new CartItem
                {
                    Id = Guid.NewGuid(),
                    CartId = cart.Id,
                    ProductId = dto.ProductId,
                    Quantity = dto.Quantity,
                    CreatedAt = DateTime.UtcNow
                };

                cart.Items.Add(item);
            }

            await _cartRepository.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task UpdateItemQuantityAsync(Guid userId, Guid cartItemId, int quantity)
        {
            if (quantity <= 0) throw new DomainException("Quantity must be greater than zero.");

            var cart = await _cartRepository.GetByUserIdAsync(userId).ConfigureAwait(false);
            if (cart == null) throw new DomainException("Cart not found.");

            var item = cart.Items?.FirstOrDefault(i => i.Id == cartItemId);
            if (item == null) throw new DomainException("Cart item not found.");

            var inventory = await _inventoryRepository.GetByProductIdAsync(item.ProductId).ConfigureAwait(false);
            if (inventory == null) throw new DomainException("Inventory record not found.");
            if (inventory.Quantity < quantity) throw new DomainException("Insufficient stock.");

            item.Quantity = quantity;
            _cartRepository.Update(cart);
            await _cartRepository.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task RemoveItemAsync(Guid userId, Guid cartItemId)
        {
            var cart = await _cartRepository.GetByUserIdAsync(userId).ConfigureAwait(false);
            if (cart == null) throw new DomainException("Cart not found.");

            var item = cart.Items?.FirstOrDefault(i => i.Id == cartItemId);
            if (item == null) throw new DomainException("Cart item not found.");

            cart.Items.Remove(item);
            _cartRepository.Update(cart);
            await _cartRepository.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task ClearCartAsync(Guid userId)
        {
            var cart = await _cartRepository.GetByUserIdAsync(userId).ConfigureAwait(false);
            if (cart == null) return;

            cart.Items.Clear();
            _cartRepository.Update(cart);
            await _cartRepository.SaveChangesAsync().ConfigureAwait(false);
        }

        private async Task<CartDto> BuildCartDtoAsync(Cart cart)
        {
            // ensure items include product info
            // cart may be no-tracking; we will query products for prices
            var dto = new CartDto { Id = cart.Id, UserId = cart.UserId };
            decimal total = 0m;

            foreach (var item in cart.Items ?? Enumerable.Empty<CartItem>())
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId).ConfigureAwait(false);
                var unitPrice = product?.Price ?? 0m;
                var itemDto = new CartItemDto { Id = item.Id, ProductId = item.ProductId, ProductName = product?.Name, ImageUrl = product?.ImageUrl, Quantity = item.Quantity, UnitPrice = unitPrice };
                dto.Items.Add(itemDto);
                total += unitPrice * item.Quantity;
            }

            dto.Total = total;
            return dto;
        }
    }
}
