using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using OrderService.Application.DTOs.Order;
using OrderService.Application.Interfaces.Clients;
using OrderService.Application.Interfaces.Repositories;
using OrderService.Application.Interfaces.Services;
using OrderService.Domain.Entities;

namespace OrderService.Application.Services
{
    public class OrderServiceImpl : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ICartRepository _cartRepository;
        private readonly IIdempotencyRepository _idempotencyRepository;
        private readonly IOutboxRepository _outboxRepository;
        private readonly IProductServiceClient _productServiceClient;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IOrderNotificationService _notificationService;
        private readonly IPaymentServiceClient _paymentServiceClient;

        public OrderServiceImpl(
            IOrderRepository orderRepository,
            ICartRepository cartRepository,
            IIdempotencyRepository idempotencyRepository,
            IOutboxRepository outboxRepository,
            IProductServiceClient productServiceClient,
            IUnitOfWork unitOfWork,
            IOrderNotificationService notificationService,
            IPaymentServiceClient paymentServiceClient)
        {
            _orderRepository = orderRepository;
            _cartRepository = cartRepository;
            _idempotencyRepository = idempotencyRepository;
            _outboxRepository = outboxRepository;
            _productServiceClient = productServiceClient;
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
            _paymentServiceClient = paymentServiceClient;
        }

        public async Task<OrderDto> CreateOrderAsync(Guid userId, CreateOrderDto createOrderDto, string idempotencyKey, string requestPath)
        {
            // 1. Idempotency Check
            var idempotencyRecord = await _idempotencyRepository.GetAsync(userId, idempotencyKey, requestPath);
            if (idempotencyRecord != null)
            {
                if (idempotencyRecord.Succeeded && idempotencyRecord.OrderId.HasValue)
                {
                    return await GetOrderAsync(idempotencyRecord.OrderId.Value, userId);
                }
                throw new InvalidOperationException("Previous request with same idempotency key is still processing or failed.");
            }

            // Create pending idempotency record
            idempotencyRecord = new IdempotencyRecord
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Key = idempotencyKey,
                RequestPath = requestPath,
                CreatedAt = DateTime.UtcNow,
                Succeeded = false
            };
            await _idempotencyRepository.AddAsync(idempotencyRecord);
            await _unitOfWork.SaveChangesAsync(); // save so it locks the key for concurrent requests

            try
            {
                // 2. Fetch Cart and Validate
                var cart = await _cartRepository.GetByUserIdAsync(userId);
                if (cart == null || !cart.Items.Any())
                {
                    throw new InvalidOperationException("Cart is empty.");
                }

                var productIds = cart.Items.Select(i => i.ProductId).Distinct().ToList();
                var products = (await _productServiceClient.GetBulkProductsAsync(productIds))
                    .ToDictionary(p => p.Id);

                var reservationId = Guid.NewGuid();

                // 3. Create Order Entity
                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ReservationId = reservationId,
                    Status = 0, // Pending
                    CreatedAt = DateTime.UtcNow,
                    ShippingAddressLine1 = createOrderDto.ShippingAddressLine1,
                    ShippingAddressLine2 = createOrderDto.ShippingAddressLine2,
                    ShippingCity = createOrderDto.ShippingCity,
                    ShippingState = createOrderDto.ShippingState,
                    ShippingPostalCode = createOrderDto.ShippingPostalCode,
                    ShippingCountry = createOrderDto.ShippingCountry,
                    ShippingPhoneNumber = createOrderDto.ShippingPhoneNumber,
                };

                decimal totalAmount = 0;
                var reservationItems = new List<(Guid ProductId, int Quantity)>();

                foreach (var cartItem in cart.Items)
                {
                    if (!products.TryGetValue(cartItem.ProductId, out var product))
                    {
                        throw new InvalidOperationException($"Product {cartItem.ProductId} not found.");
                    }

                    var orderItem = new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        ProductId = cartItem.ProductId,
                        ProductName = product.Name,
                        ProductImageUrl = product.ImageUrl,
                        Quantity = cartItem.Quantity,
                        UnitPrice = product.Price
                    };
                    order.Items.Add(orderItem);
                    totalAmount += orderItem.UnitPrice * orderItem.Quantity;
                    reservationItems.Add((cartItem.ProductId, cartItem.Quantity));
                }

                order.TotalAmount = totalAmount;

                // 4. Reserve Inventory (Synchronous part of Saga)
                var reserved = await _productServiceClient.ReserveInventoryAsync(reservationId, reservationItems);
                if (!reserved)
                {
                    throw new InvalidOperationException("Insufficient inventory.");
                }

                // 5. Save Order and Outbox Message
                await _orderRepository.AddAsync(order);
                cart.Items.Clear(); // Empty cart
                await _cartRepository.UpdateAsync(cart);

                idempotencyRecord.OrderId = order.Id;
                idempotencyRecord.Succeeded = true;
                await _idempotencyRepository.UpdateAsync(idempotencyRecord);

                var outboxMessage = new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    EventType = "OrderCreated",
                    Payload = JsonSerializer.Serialize(new { OrderId = order.Id, ReservationId = reservationId, UserId = userId }),
                    CreatedAt = DateTime.UtcNow,
                    Processed = false,
                    RetryCount = 0
                };
                await _outboxRepository.AddAsync(outboxMessage);

                try
                {
                    await _unitOfWork.SaveChangesAsync();
                }
                catch
                {
                    // Best effort compensation if OrderService DB fails
                    try
                    {
                        await _productServiceClient.ReleaseInventoryAsync(reservationId);
                    }
                    catch
                    {
                        // Ignore, reservation will expire on ProductService side
                    }
                    throw;
                }

                return MapToDto(order);
            }
            catch
            {
                // If it fails, ideally we should release inventory, or let it expire.
                throw;
            }
        }

        public async Task<OrderDto> GetOrderAsync(Guid id, Guid userId)
        {
            var order = await _orderRepository.GetByIdAsync(id);
            if (order == null || order.UserId != userId)
                throw new InvalidOperationException("Order not found");

            return MapToDto(order);
        }

        public async Task<IEnumerable<OrderDto>> GetUserOrdersAsync(Guid userId)
        {
            var orders = await _orderRepository.GetByUserIdAsync(userId);
            return orders.Select(MapToDto).OrderByDescending(o => o.CreatedAt);
        }

        public async Task<OrderDto> GetOrderForAdminAsync(Guid id)
        {
            var order = await _orderRepository.GetByIdAsync(id);
            if (order == null)
                throw new InvalidOperationException("Order not found");

            return MapToDto(order);
        }

        public async Task<OrderDto> UpdateOrderStatusAsync(Guid id, int newStatus)
        {
            var order = await _orderRepository.GetByIdAsync(id);
            if (order == null)
                throw new InvalidOperationException("Order not found");

            // Terminal states
            if (order.Status == 4 || order.Status == 5)
            {
                throw new InvalidOperationException("Cannot change status of a terminal order.");
            }

            // Valid transitions
            bool validTransition = false;
            switch (order.Status)
            {
                case 0: // Pending
                    validTransition = (newStatus == 1 || newStatus == 2 || newStatus == 5);
                    break;
                case 1: // Confirmed
                    validTransition = (newStatus == 2 || newStatus == 5);
                    break;
                case 2: // Processing
                    validTransition = (newStatus == 3 || newStatus == 5);
                    break;
                case 3: // Shipped
                    validTransition = (newStatus == 4 || newStatus == 5);
                    break;
            }

            if (!validTransition)
            {
                throw new InvalidOperationException($"Invalid status transition from {order.Status} to {newStatus}");
            }

            if (newStatus == 2 && !order.IsPaymentSuccessful)
            {
                throw new InvalidOperationException("Cannot change status to Processing without successful payment.");
            }

            order.Status = newStatus;
            order.UpdatedAt = DateTime.UtcNow;

            if (order.Status == 1)
            {
                await TryFinalizeOrderAsync(order);
            }
            else if (order.Status == 5)
            {
                await PublishOrderCancelledEventAsync(order);
            }

            await _orderRepository.UpdateAsync(order);
            await _unitOfWork.SaveChangesAsync();

            await _notificationService.SendOrderStatusUpdatedNotificationAsync(order.UserId, order.Id, order.Status);

            return MapToDto(order);
        }

        public async Task HandlePaymentSuccessAsync(Guid id)
        {
            var order = await _orderRepository.GetByIdAsync(id);
            if (order == null) throw new InvalidOperationException("Order not found");
            
            if (order.IsPaymentSuccessful) return;

            order.IsPaymentSuccessful = true;
            order.UpdatedAt = DateTime.UtcNow;

            await TryFinalizeOrderAsync(order);

            await _orderRepository.UpdateAsync(order);
            await _unitOfWork.SaveChangesAsync();
        }

        private async Task TryFinalizeOrderAsync(Order order)
        {
            if (order.Status == 1 && order.IsPaymentSuccessful)
            {
                order.Status = 2; // Finalized / Processing
                order.UpdatedAt = DateTime.UtcNow;

                var outboxMessage = new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    EventType = "OrderFinalized",
                    Payload = JsonSerializer.Serialize(new { OrderId = order.Id, ReservationId = order.ReservationId, UserId = order.UserId }),
                    CreatedAt = DateTime.UtcNow,
                    Processed = false,
                    RetryCount = 0
                };
                await _outboxRepository.AddAsync(outboxMessage);
            }
        }

        public async Task<OrderDto> CancelOrderAsync(Guid id, Guid userId)
        {
            var order = await _orderRepository.GetByIdAsync(id);
            if (order == null || order.UserId != userId)
                throw new InvalidOperationException("Order not found");

            if (order.Status == 2 || order.Status == 3 || order.Status == 4 || order.Status == 5)
                throw new InvalidOperationException($"Cannot cancel order in status {order.Status}");

            order.Status = 5; // Cancelled
            order.UpdatedAt = DateTime.UtcNow;

            await _orderRepository.UpdateAsync(order);

            await PublishOrderCancelledEventAsync(order);

            await _unitOfWork.SaveChangesAsync();

            await _notificationService.SendOrderStatusUpdatedNotificationAsync(order.UserId, order.Id, 5);

            return MapToDto(order);
        }

        private async Task PublishOrderCancelledEventAsync(Order order)
        {
            // Get actual payment state
            bool isActuallyPaid = order.IsPaymentSuccessful;
            try
            {
                var paymentDto = await _paymentServiceClient.GetPaymentByOrderIdAsync(order.Id);
                if (paymentDto != null)
                {
                    isActuallyPaid = paymentDto.Status == 1; // 1 is Successful
                }
            }
            catch
            {
                // Fallback to order property if service is unreachable
            }

            // Create OrderCancelled OutboxMessage
            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                EventType = "OrderCancelled",
                Payload = JsonSerializer.Serialize(new { 
                    OrderId = order.Id, 
                    ReservationId = order.ReservationId, 
                    UserId = order.UserId, 
                    IsPaymentSuccessful = isActuallyPaid
                }),
                CreatedAt = DateTime.UtcNow,
                Processed = false,
                RetryCount = 0
            };
            await _outboxRepository.AddAsync(outboxMessage);
        }

        private OrderDto MapToDto(Order order)
        {
            return new OrderDto
            {
                Id = order.Id,
                UserId = order.UserId,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                CreatedAt = order.CreatedAt,
                ShippingAddressLine1 = order.ShippingAddressLine1,
                ShippingAddressLine2 = order.ShippingAddressLine2,
                ShippingCity = order.ShippingCity,
                ShippingState = order.ShippingState,
                ShippingPostalCode = order.ShippingPostalCode,
                ShippingCountry = order.ShippingCountry,
                ShippingPhoneNumber = order.ShippingPhoneNumber,
                Items = order.Items.Select(i => new OrderItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    ProductImageUrl = i.ProductImageUrl,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            };
        }
    }
}
