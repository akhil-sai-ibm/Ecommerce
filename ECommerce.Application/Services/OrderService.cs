using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Application.DTOs.Orders;
using ECommerce.Application.Interfaces.Repositories;
using ECommerce.Application.Interfaces.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ICartRepository _cartRepository;
        private readonly IProductRepository _productRepository;
        private readonly IInventoryRepository _inventoryRepository;
        private readonly IIdempotencyRepository _idempotencyRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IOrderNotificationService? _notificationService;
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            IOrderRepository orderRepository,
            ICartRepository cartRepository,
            IProductRepository productRepository,
            IInventoryRepository inventoryRepository,
            IIdempotencyRepository idempotencyRepository,
            IUnitOfWork unitOfWork,
            ILogger<OrderService> logger,
            IOrderNotificationService? notificationService = null)
        {
            _orderRepository = orderRepository;
            _cartRepository = cartRepository;
            _productRepository = productRepository;
            _inventoryRepository = inventoryRepository;
            _idempotencyRepository = idempotencyRepository;
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<OrderDto?> GetByIdAsync(Guid id)
        {
            var order = await _orderRepository.GetByIdWithDetailsAsync(id).ConfigureAwait(false);
            if (order == null) return null;
            return Map(order);
        }

        public async Task<IEnumerable<OrderDto>> GetByUserIdAsync(Guid userId)
        {
            var orders = await _orderRepository.GetByUserIdAsync(userId).ConfigureAwait(false);
            return orders.Select(Map);
        }

        public async Task<IEnumerable<OrderDto>> GetAllAsync()
        {
            var orders = await _orderRepository.GetAllWithDetailsAsync().ConfigureAwait(false);
            return orders.Select(Map);
        }

        public async Task<OrderDto> CreateOrderFromCartAsync(Guid userId, string? idempotencyKey = null)
        {
            // Validate idempotency key format (if provided)
            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                if (idempotencyKey.Length > 128)
                {
                    throw new DomainException("Idempotency key must be 128 characters or less.");
                }

                // Check for existing idempotency record
                var existingRecord = await _idempotencyRepository.GetByKeyAndUserIdAsync(idempotencyKey, userId).ConfigureAwait(false);
                if (existingRecord != null)
                {
                    if (existingRecord.Succeeded)
                    {
                        _logger.LogInformation("Idempotency hit for user {UserId} with key {Key}. Returning cached order {OrderId}", 
                            userId, idempotencyKey, existingRecord.OrderId);
                        // Return the cached order
                        return (await GetByIdAsync(existingRecord.OrderId.Value).ConfigureAwait(false))!;
                    }
                    else
                    {
                        // Previous attempt failed; allow retry
                        _logger.LogInformation("Previous idempotent request for user {UserId} with key {Key} failed. Allowing retry.", 
                            userId, idempotencyKey);
                    }
                }
            }

            _logger.LogInformation("Checkout started for user {UserId}" + (idempotencyKey != null ? " with key {Key}" : ""), userId, idempotencyKey ?? "(none)");

            IdempotencyRecord? idempotencyRecord = null;

            await using var transaction = await _unitOfWork.BeginTransactionAsync().ConfigureAwait(false);
            try
            {
                // Create idempotency record if key provided
                if (!string.IsNullOrWhiteSpace(idempotencyKey))
                {
                    idempotencyRecord = new IdempotencyRecord
                    {
                        Id = Guid.NewGuid(),
                        Key = idempotencyKey,
                        UserId = userId,
                        Endpoint = "/api/orders/checkout",
                        Method = "POST",
                        CreatedAt = DateTime.UtcNow,
                        Succeeded = false,
                        OrderId = null,
                        ErrorMessage = null,
                        ResponseStatusCode = 0
                    };
                    await _idempotencyRepository.AddAsync(idempotencyRecord).ConfigureAwait(false);
                }
                var userCart = await _cartRepository.GetByUserIdAsync(userId).ConfigureAwait(false);
                if (userCart == null)
                {
                    _logger.LogWarning("Cart not found for user {UserId}", userId);
                    throw new DomainException("Cart not found.");
                }

                var cart = await _cartRepository.GetWithItemsAsync(userCart.Id).ConfigureAwait(false);
                if (cart == null || cart.Items == null || !cart.Items.Any())
                {
                    _logger.LogWarning("Cart is empty for user {UserId}", userId);
                    throw new DomainException("Cart is empty.");
                }

                // Validate products and inventory
                decimal total = 0m;
                var orderItems = new List<OrderItem>();

                foreach (var ci in cart.Items)
                {
                    var product = await _productRepository.GetByIdAsync(ci.ProductId).ConfigureAwait(false);
                    if (product == null)
                    {
                        _logger.LogWarning("Product {ProductId} not found during checkout for user {UserId}", ci.ProductId, userId);
                        throw new DomainException($"Product {ci.ProductId} not found.");
                    }

                    if (product.Status != ProductStatus.Active)
                    {
                        _logger.LogWarning("Product {ProductId} ({ProductName}) is not available (status: {Status})", product.Id, product.Name, product.Status);
                        throw new DomainException($"Product {product.Name} is not available.");
                    }

                    var inventory = await _inventoryRepository.GetByProductIdAsync(ci.ProductId).ConfigureAwait(false);
                    if (inventory == null)
                    {
                        _logger.LogError("Inventory record not found for product {ProductId}", ci.ProductId);
                        throw new DomainException($"Inventory for product {product.Name} not found.");
                    }

                    if (inventory.Quantity < ci.Quantity)
                    {
                        _logger.LogWarning("Insufficient stock for product {ProductId} ({ProductName}). Required: {Required}, Available: {Available}",
                            ci.ProductId, product.Name, ci.Quantity, inventory.Quantity);
                        throw new DomainException($"Insufficient stock for product {product.Name}.");
                    }

                    var unitPrice = product.Price;
                    total += unitPrice * ci.Quantity;

                    orderItems.Add(new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        ProductId = product.Id,
                        Quantity = ci.Quantity,
                        UnitPrice = unitPrice
                    });
                }

                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Status = OrderStatus.Pending,
                    TotalAmount = total,
                    CreatedAt = DateTime.UtcNow,
                    Items = orderItems
                };

                await _orderRepository.AddAsync(order).ConfigureAwait(false);

                // Decrement inventory - each update is atomic at the database level
                // with the check constraint preventing negative quantities
                foreach (var oi in orderItems)
                {
                    var inv = await _inventoryRepository.GetByProductIdAsync(oi.ProductId).ConfigureAwait(false);
                    if (inv == null)
                    {
                        _logger.LogError("Inventory record missing during checkout for product {ProductId}", oi.ProductId);
                        throw new DomainException("Inventory record missing during checkout.");
                    }

                    if (inv.Quantity - oi.Quantity < 0)
                    {
                        _logger.LogWarning("Stock race condition: Inventory for product {ProductId} is insufficient (required: {Required}, available: {Available})",
                            oi.ProductId, oi.Quantity, inv.Quantity);
                        throw new DomainException($"Insufficient stock for product during checkout (race condition detected).");
                    }

                    inv.Quantity -= oi.Quantity;
                    _inventoryRepository.Update(inv);
                }

                // Clear cart
                cart.Items.Clear();
                _cartRepository.Update(cart);

                // Persist all changes atomically
                await _unitOfWork.SaveChangesAsync().ConfigureAwait(false);
                await transaction.CommitAsync().ConfigureAwait(false);

                // Update idempotency record to mark as successful
                if (idempotencyRecord != null)
                {
                    idempotencyRecord.Succeeded = true;
                    idempotencyRecord.OrderId = order.Id;
                    idempotencyRecord.ResponseStatusCode = 201;
                    _idempotencyRepository.Update(idempotencyRecord);
                    await _idempotencyRepository.SaveChangesAsync().ConfigureAwait(false);
                }

                _logger.LogInformation("Checkout succeeded for user {UserId}. Order {OrderId} created with total {Total}", 
                    userId, order.Id, order.TotalAmount);

                return Map(order);
            }
            catch (Exception ex)
            {
                // Handle different types of exceptions without referencing EF Core directly
                string exceptionTypeName = ex.GetType().Name;

                if (exceptionTypeName == "DbUpdateConcurrencyException")
                {
                    // Inventory was modified concurrently - this is the expected way to detect race conditions
                    _logger.LogWarning(ex, "Inventory concurrency conflict during checkout for user {UserId}. Another transaction modified the inventory.", userId);
                    await transaction.RollbackAsync().ConfigureAwait(false);
                    throw new DomainException("Unable to complete checkout due to concurrent inventory update. Please try again.");
                }
                else if (exceptionTypeName == "DbUpdateException" && 
                         (ex.InnerException?.Message?.Contains("IX_IdempotencyRecord_UserIdKey", StringComparison.OrdinalIgnoreCase) ?? false))
                {
                    // Duplicate idempotency key constraint violation
                    // This means another concurrent request already created an idempotency record
                    _logger.LogWarning(ex, "Duplicate idempotency key detected for user {UserId} with key {Key}. Another request is processing the same checkout.", 
                        userId, idempotencyKey);
                    await transaction.RollbackAsync().ConfigureAwait(false);
                    throw new DomainException("A checkout request with this idempotency key is already being processed. Please wait and retry with the same key.");
                }

                // General error handling
                _logger.LogError(ex, "Checkout failed for user {UserId}. Rolling back transaction.", userId);

                // Update idempotency record to mark as failed
                if (idempotencyRecord != null)
                {
                    idempotencyRecord.Succeeded = false;
                    idempotencyRecord.ErrorMessage = ex.Message;
                    idempotencyRecord.ResponseStatusCode = 400;
                    _idempotencyRepository.Update(idempotencyRecord);
                    try
                    {
                        await _idempotencyRepository.SaveChangesAsync().ConfigureAwait(false);
                    }
                    catch
                    {
                        // Ignore errors updating idempotency record; don't mask the original error
                        _logger.LogWarning("Failed to update idempotency record for failed checkout");
                    }
                }

                await transaction.RollbackAsync().ConfigureAwait(false);
                throw;
            }
        }

        public async Task UpdateOrderStatusAsync(Guid orderId, int status)
        {
            if (!Enum.IsDefined(typeof(OrderStatus), status))
            {
                _logger.LogWarning("Invalid order status {Status} for order {OrderId}", status, orderId);
                throw new DomainException("Invalid order status.");
            }

            var order = await _orderRepository.GetByIdAsync(orderId).ConfigureAwait(false);
            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found for status update", orderId);
                throw new DomainException("Order not found.");
            }

            var newStatus = (OrderStatus)status;

            // Validate state transition
            if (!IsValidStatusTransition(order.Status, newStatus))
            {
                _logger.LogWarning("Invalid status transition for order {OrderId}: {CurrentStatus} → {NewStatus}",
                    orderId, order.Status, newStatus);
                throw new DomainException($"Cannot transition from {order.Status} to {newStatus}.");
            }

            order.Status = newStatus;
            order.UpdatedAt = DateTime.UtcNow;
            _orderRepository.Update(order);
            await _orderRepository.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Order {OrderId} status updated to {Status}", orderId, newStatus);

            // Send notification after successful status change
            if (_notificationService != null)
            {
                try
                {
                    await _notificationService.NotifyOrderStatusChangedAsync(order.UserId, orderId, newStatus)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send order status notification for order {OrderId}", orderId);
                    // Don't throw - notification failure should not fail order status update
                }
            }
        }


        public async Task CancelOrderAsync(Guid orderId)
        {
            var order = await _orderRepository.GetByIdWithDetailsAsync(orderId).ConfigureAwait(false);
            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found for cancellation", orderId);
                throw new DomainException("Order not found.");
            }

            // Prevent cancellation of shipped/delivered orders
            if (order.Status == OrderStatus.Shipped || order.Status == OrderStatus.Delivered)
            {
                _logger.LogWarning("Cannot cancel order {OrderId} with status {Status}", orderId, order.Status);
                throw new DomainException("Cannot cancel an order that has already shipped or delivered.");
            }

            // Prevent double-cancellation
            if (order.Status == OrderStatus.Cancelled)
            {
                _logger.LogInformation("Order {OrderId} is already cancelled", orderId);
                throw new DomainException("Order is already cancelled.");
            }

            await using var transaction = await _unitOfWork.BeginTransactionAsync().ConfigureAwait(false);
            try
            {
                order.Status = OrderStatus.Cancelled;
                order.UpdatedAt = DateTime.UtcNow;
                _orderRepository.Update(order);

                // Restore inventory for all order items
                if (order.Items != null && order.Items.Any())
                {
                    foreach (var oi in order.Items)
                    {
                        var inventory = await _inventoryRepository.GetByProductIdAsync(oi.ProductId).ConfigureAwait(false);
                        if (inventory != null)
                        {
                            inventory.Quantity += oi.Quantity;
                            _inventoryRepository.Update(inventory);
                            _logger.LogInformation("Restored inventory for product {ProductId}: +{Quantity} units", oi.ProductId, oi.Quantity);
                        }
                    }
                }

                await _unitOfWork.SaveChangesAsync().ConfigureAwait(false);
                await transaction.CommitAsync().ConfigureAwait(false);

                _logger.LogInformation("Order {OrderId} cancelled successfully. Inventory restored.", orderId);

                // Send cancellation notification after successful commit
                if (_notificationService != null)
                {
                    try
                    {
                        await _notificationService.NotifyOrderCancelledAsync(order.UserId, orderId)
                            .ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send order cancellation notification for order {OrderId}", orderId);
                        // Don't throw - notification failure should not affect successful cancellation
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel order {OrderId}. Rolling back transaction.", orderId);
                await transaction.RollbackAsync().ConfigureAwait(false);
                throw;
            }
        }


        /// <summary>
        /// Determines if a status transition is valid.
        /// Valid flow: Pending → Confirmed → Processing → Shipped → Delivered
        /// Cancellation allowed from: Pending, Confirmed, Processing
        /// </summary>
        private static bool IsValidStatusTransition(OrderStatus from, OrderStatus to)
        {
            // Cannot transition from/to the same status
            if (from == to) return false;

            // Cannot transition from Cancelled, Shipped, or Delivered to other states (except same)
            if (from == OrderStatus.Cancelled || from == OrderStatus.Shipped || from == OrderStatus.Delivered)
            {
                return false;
            }

            // Valid forward transitions in the main workflow
            if (from == OrderStatus.Pending && to == OrderStatus.Confirmed) return true;
            if (from == OrderStatus.Confirmed && to == OrderStatus.Processing) return true;
            if (from == OrderStatus.Processing && to == OrderStatus.Shipped) return true;
            if (from == OrderStatus.Shipped && to == OrderStatus.Delivered) return true;

            return false;
        }

        private static OrderDto Map(Order o)
        {
            return new OrderDto
            {
                Id = o.Id,
                UserId = o.UserId,
                Status = o.Status,
                TotalAmount = o.TotalAmount,
                Items = o.Items?.Select(i => new OrderItemDto { Id = i.Id, ProductId = i.ProductId, Quantity = i.Quantity, UnitPrice = i.UnitPrice }).ToList() ?? new List<OrderItemDto>(),
                PaymentId = o.Payment?.Id
            };
        }
    }
}
