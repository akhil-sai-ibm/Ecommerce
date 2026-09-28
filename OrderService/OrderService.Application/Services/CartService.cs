using System;
using System.Linq;
using System.Threading.Tasks;
using OrderService.Application.DTOs.Cart;
using OrderService.Application.Interfaces.Clients;
using OrderService.Application.Interfaces.Repositories;
using OrderService.Application.Interfaces.Services;
using OrderService.Domain.Entities;

namespace OrderService.Application.Services
{
    public class CartService : ICartService
    {
        private readonly ICartRepository _cartRepository;
        private readonly IProductServiceClient _productServiceClient;
        private readonly IUnitOfWork _unitOfWork;

        public CartService(
            ICartRepository cartRepository,
            IProductServiceClient productServiceClient,
            IUnitOfWork unitOfWork)
        {
            _cartRepository = cartRepository;
            _productServiceClient = productServiceClient;
            _unitOfWork = unitOfWork;
        }

        public async Task<CartDto> GetCartAsync(Guid userId)
        {
            var semaphore = _cartLocks.GetOrAdd(userId, _ => new System.Threading.SemaphoreSlim(1, 1));
            await semaphore.WaitAsync();
            try
            {
                var cart = await _cartRepository.GetByUserIdAsync(userId);
                if (cart == null)
                {
                    cart = new Cart
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _cartRepository.AddAsync(cart);
                    await _unitOfWork.SaveChangesAsync();
                }

                return await MapToDtoAsync(cart);
            }
            finally
            {
                semaphore.Release();
            }
        }

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, System.Threading.SemaphoreSlim> _cartLocks = new();

        public async Task<CartDto> AddToCartAsync(Guid userId, AddToCartDto addToCartDto)
        {
            var semaphore = _cartLocks.GetOrAdd(userId, _ => new System.Threading.SemaphoreSlim(1, 1));
            await semaphore.WaitAsync();
            try
            {
                var cart = await _cartRepository.GetByUserIdAsync(userId);
                if (cart == null)
                {
                    cart = new Cart
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _cartRepository.AddAsync(cart);
                }

                var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == addToCartDto.ProductId);
                if (existingItem != null)
                {
                    existingItem.Quantity += addToCartDto.Quantity;
                }
                else
                {
                    cart.Items.Add(new CartItem
                    {
                        Id = Guid.NewGuid(),
                        CartId = cart.Id,
                        ProductId = addToCartDto.ProductId,
                        Quantity = addToCartDto.Quantity,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                cart.UpdatedAt = DateTime.UtcNow;
                
                await _unitOfWork.SaveChangesAsync();

                return await MapToDtoAsync(cart);
            }
            finally
            {
                semaphore.Release();
            }
        }

        public async Task<CartDto> RemoveFromCartAsync(Guid userId, Guid productId)
        {
            var semaphore = _cartLocks.GetOrAdd(userId, _ => new System.Threading.SemaphoreSlim(1, 1));
            await semaphore.WaitAsync();
            try
            {
                var cart = await _cartRepository.GetByUserIdAsync(userId);
                if (cart == null)
                    return new CartDto { UserId = userId };

                var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
                if (item != null)
                {
                    cart.Items.Remove(item);
                    cart.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.SaveChangesAsync();
                }

                return await MapToDtoAsync(cart);
            }
            finally
            {
                semaphore.Release();
            }
        }

        public async Task ClearCartAsync(Guid userId)
        {
            var semaphore = _cartLocks.GetOrAdd(userId, _ => new System.Threading.SemaphoreSlim(1, 1));
            await semaphore.WaitAsync();
            try
            {
                var cart = await _cartRepository.GetByUserIdAsync(userId);
                if (cart != null)
                {
                    cart.Items.Clear();
                    cart.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.SaveChangesAsync();
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        private async Task<CartDto> MapToDtoAsync(Cart cart)
        {
            var dto = new CartDto
            {
                Id = cart.Id,
                UserId = cart.UserId
            };

            if (cart.Items.Any())
            {
                var productIds = cart.Items.Select(i => i.ProductId).Distinct();
                var products = (await _productServiceClient.GetBulkProductsAsync(productIds))
                    .ToDictionary(p => p.Id);

                dto.Items = cart.Items.Select(i =>
                {
                    var product = products.ContainsKey(i.ProductId) ? products[i.ProductId] : null;
                    return new CartItemDto
                    {
                        Id = i.Id,
                        ProductId = i.ProductId,
                        Quantity = i.Quantity,
                        ProductName = product?.Name ?? "Unknown Product",
                        UnitPrice = product?.Price ?? 0,
                        ProductImageUrl = product?.ImageUrl
                    };
                }).ToList();
            }

            return dto;
        }
    }
}
