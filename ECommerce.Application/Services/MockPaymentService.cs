using System;
using System.Threading.Tasks;
using ECommerce.Application.DTOs.Payments;
using ECommerce.Application.Interfaces.Repositories;
using ECommerce.Application.Interfaces.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Services
{
    /// <summary>
    /// Mock payment service for demonstration purposes.
    /// Simulates payment processing without connecting to external providers.
    /// </summary>
    public class MockPaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger<MockPaymentService> _logger;

        // Mock account balance for testing
        private const decimal MockAccountBalance = 1000000m;

        public MockPaymentService(IPaymentRepository paymentRepository, IOrderRepository orderRepository, ILogger<MockPaymentService> logger)
        {
            _paymentRepository = paymentRepository;
            _orderRepository = orderRepository;
            _logger = logger;
        }

        public async Task<PaymentDto?> GetByIdAsync(Guid id)
        {
            var payment = await _paymentRepository.GetByIdAsync(id).ConfigureAwait(false);
            if (payment == null) return null;
            return Map(payment);
        }

        public async Task<PaymentDto?> GetByOrderIdAsync(Guid orderId)
        {
            var payment = await _paymentRepository.GetByOrderIdAsync(orderId).ConfigureAwait(false);
            if (payment == null) return null;
            return Map(payment);
        }

        public async Task<PaymentDto> CreateAsync(CreatePaymentDto dto)
        {
            if (dto.Amount <= 0)
            {
                _logger.LogWarning("Payment creation rejected: invalid amount {Amount}", dto.Amount);
                throw new DomainException("Payment amount must be positive.");
            }

            var order = await _orderRepository.GetByIdAsync(dto.OrderId).ConfigureAwait(false);
            if (order == null)
            {
                _logger.LogWarning("Payment creation rejected: order {OrderId} not found", dto.OrderId);
                throw new DomainException("Order not found.");
            }

            if (order.TotalAmount != dto.Amount)
            {
                _logger.LogWarning("Payment amount {PaymentAmount} does not match order total {OrderTotal} for order {OrderId}",
                    dto.Amount, order.TotalAmount, dto.OrderId);
                throw new DomainException("Payment amount does not match order total.");
            }

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                OrderId = dto.OrderId,
                Amount = dto.Amount,
                Method = dto.Method,
                Status = PaymentStatus.Pending,
                TransactionId = dto.TransactionId ?? $"MOCK-{Guid.NewGuid():N}",
                CreatedAt = DateTime.UtcNow
            };

            await _paymentRepository.AddAsync(payment).ConfigureAwait(false);
            await _paymentRepository.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Payment {PaymentId} created for order {OrderId} with amount {Amount}", 
                payment.Id, payment.OrderId, payment.Amount);

            return Map(payment);
        }

        public async Task UpdateStatusAsync(Guid paymentId, string status)
        {
            var payment = await _paymentRepository.GetByIdAsync(paymentId).ConfigureAwait(false);
            if (payment == null)
            {
                throw new DomainException("Payment not found.");
            }

            if (!Enum.TryParse<PaymentStatus>(status, true, out var newStatus))
            {
                throw new DomainException("Invalid payment status.");
            }

            payment.Status = newStatus;
            _paymentRepository.Update(payment);
            await _paymentRepository.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Payment {PaymentId} status updated to {Status}", paymentId, newStatus);
        }

        /// <summary>
        /// Mock payment processing.
        /// Returns success if account has sufficient balance. Always succeeds for demo purposes.
        /// </summary>
        public async Task<PaymentDto> ProcessPaymentAsync(Guid paymentId)
        {
            var payment = await _paymentRepository.GetByIdAsync(paymentId).ConfigureAwait(false);
            if (payment == null)
            {
                throw new DomainException("Payment not found.");
            }

            if (payment.Status != PaymentStatus.Pending)
            {
                throw new DomainException($"Cannot process payment with status {payment.Status}.");
            }

            // Mock processor: check if account has sufficient balance
            if (MockAccountBalance < payment.Amount)
            {
                _logger.LogWarning("Mock payment {PaymentId} failed: insufficient account balance", paymentId);
                payment.Status = PaymentStatus.Failed;
                _paymentRepository.Update(payment);
                await _paymentRepository.SaveChangesAsync().ConfigureAwait(false);
                throw new DomainException("Payment failed: insufficient account balance.");
            }

            // Mock successful payment
            payment.Status = PaymentStatus.Successful;
            _paymentRepository.Update(payment);
            await _paymentRepository.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Mock payment {PaymentId} processed successfully for amount {Amount}", paymentId, payment.Amount);

            return Map(payment);
        }

        private static PaymentDto Map(Payment p)
        {
            return new PaymentDto
            {
                Id = p.Id,
                OrderId = p.OrderId,
                Amount = p.Amount,
                Method = p.Method,
                Status = p.Status,
                TransactionId = p.TransactionId
            };
        }
    }
}
