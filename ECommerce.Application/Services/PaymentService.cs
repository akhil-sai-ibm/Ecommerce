using System;
using System.Threading.Tasks;
using ECommerce.Application.DTOs.Payments;
using ECommerce.Application.Interfaces.Repositories;
using ECommerce.Application.Interfaces.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Exceptions;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IOrderRepository _orderRepository;

        public PaymentService(IPaymentRepository paymentRepository, IOrderRepository orderRepository)
        {
            _paymentRepository = paymentRepository;
            _orderRepository = orderRepository;
        }

        public async Task<PaymentDto?> GetByIdAsync(Guid id)
        {
            var p = await _paymentRepository.GetByIdAsync(id).ConfigureAwait(false);
            if (p == null) return null;
            return Map(p);
        }

        public async Task<PaymentDto?> GetByOrderIdAsync(Guid orderId)
        {
            var p = await _paymentRepository.GetByOrderIdAsync(orderId).ConfigureAwait(false);
            if (p == null) return null;
            return Map(p);
        }

        public async Task<PaymentDto> CreateAsync(CreatePaymentDto dto)
        {
            if (dto.Amount <= 0) throw new DomainException("Payment amount must be positive.");

            var order = await _orderRepository.GetByIdAsync(dto.OrderId).ConfigureAwait(false);
            if (order == null) throw new DomainException("Order not found.");
            if (order.TotalAmount != dto.Amount) throw new DomainException("Payment amount does not match order total.");

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                OrderId = dto.OrderId,
                Amount = dto.Amount,
                Method = dto.Method,
                Status = PaymentStatus.Pending,
                TransactionId = dto.TransactionId,
                CreatedAt = DateTime.UtcNow
            };

            await _paymentRepository.AddAsync(payment).ConfigureAwait(false);
            await _paymentRepository.SaveChangesAsync().ConfigureAwait(false);

            return Map(payment);
        }

        public async Task UpdateStatusAsync(Guid paymentId, string status)
        {
            var payment = await _paymentRepository.GetByIdAsync(paymentId).ConfigureAwait(false);
            if (payment == null) throw new DomainException("Payment not found.");

            if (!Enum.TryParse<PaymentStatus>(status, true, out var parsed)) throw new DomainException("Invalid payment status.");

            payment.Status = parsed;
            _paymentRepository.Update(payment);
            await _paymentRepository.SaveChangesAsync().ConfigureAwait(false);
        }

        public virtual async Task<PaymentDto> ProcessPaymentAsync(Guid paymentId)
        {
            throw new NotImplementedException("ProcessPaymentAsync must be implemented in a subclass or payment provider.");
        }

        private static PaymentDto Map(Payment p) => new PaymentDto { Id = p.Id, OrderId = p.OrderId, Amount = p.Amount, Method = p.Method, Status = p.Status, TransactionId = p.TransactionId };
    }
}

