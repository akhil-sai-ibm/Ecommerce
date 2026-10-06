using System;
using System.Threading.Tasks;
using PaymentService.Application.DTOs.Payments;
using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Application.Interfaces.Services;
using PaymentService.Application.Interfaces.Clients;
using PaymentService.Application.Interfaces.Providers;
using PaymentService.Domain.Entities;

namespace PaymentService.Application.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IOrderServiceClient _orderServiceClient;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIdempotencyRepository _idempotencyRepository;
        private readonly IPaymentProvider _paymentProvider;
        private readonly IOutboxRepository _outboxRepository;

        public PaymentService(
            IPaymentRepository paymentRepository, 
            IOrderServiceClient orderServiceClient,
            IUnitOfWork unitOfWork,
            IIdempotencyRepository idempotencyRepository,
            IPaymentProvider paymentProvider,
            IOutboxRepository outboxRepository)
        {
            _paymentRepository = paymentRepository;
            _orderServiceClient = orderServiceClient;
            _unitOfWork = unitOfWork;
            _idempotencyRepository = idempotencyRepository;
            _paymentProvider = paymentProvider;
            _outboxRepository = outboxRepository;
        }

        public async Task<PaymentDto> CreatePaymentAsync(Guid userId, CreatePaymentDto dto, string idempotencyKey, string requestPath)
        {
            // Idempotency Check
            var idempotencyRecord = await _idempotencyRepository.GetAsync(userId, idempotencyKey, requestPath);
            if (idempotencyRecord != null)
            {
                if (idempotencyRecord.Succeeded && idempotencyRecord.OrderId.HasValue)
                {
                    return await GetOrderPaymentAsync(userId, idempotencyRecord.OrderId.Value);
                }
                throw new InvalidOperationException("Previous request with same idempotency key is still processing or failed.");
            }

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
            await _unitOfWork.SaveChangesAsync();

            bool ownsOrder = await _orderServiceClient.VerifyOrderOwnershipAsync(dto.OrderId, userId);
            if (!ownsOrder)
                throw new InvalidOperationException("Order not found or access denied.");

            var status = await _orderServiceClient.GetOrderStatusAsync(dto.OrderId);
            if (status >= 2 && status != 5) 
                throw new InvalidOperationException("Order is already processing or completed.");

            // Retrying a failed payment creates a new Payment record
            var existingPayment = await _paymentRepository.GetByOrderIdAsync(dto.OrderId);
            if (existingPayment != null)
            {
                if (existingPayment.Status == "Success")
                    throw new InvalidOperationException("Payment already succeeded.");
                
                _paymentRepository.Delete(existingPayment);
            }

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                OrderId = dto.OrderId,
                Amount = dto.Amount,
                Status = "Pending",
                ProcessedAt = DateTime.UtcNow,
                Method = dto.Method
            };

            await _paymentRepository.AddAsync(payment);

            idempotencyRecord.OrderId = dto.OrderId;
            idempotencyRecord.Succeeded = true;
            await _idempotencyRepository.UpdateAsync(idempotencyRecord);

            await _unitOfWork.SaveChangesAsync();

            return MapToDto(payment);
        }

        public async Task<PaymentDto> ProcessPaymentAsync(Guid userId, Guid paymentId)
        {
            var payment = await _paymentRepository.GetByIdAsync(paymentId);
            if (payment == null)
                throw new InvalidOperationException("Payment not found.");

            bool ownsOrder = await _orderServiceClient.VerifyOrderOwnershipAsync(payment.OrderId, userId);
            if (!ownsOrder)
                throw new InvalidOperationException("Order not found or access denied.");

            if (payment.Status != "Pending")
                throw new InvalidOperationException("Payment is not in a valid state to be processed.");

            var result = await _paymentProvider.ProcessPaymentAsync(payment.Amount);
            
            payment.Status = result.IsSuccess ? "Success" : "Failed";
            payment.ProcessedAt = DateTime.UtcNow;
            payment.TransactionId = result.TransactionId;

            await _paymentRepository.UpdateAsync(payment);

            if (result.IsSuccess)
            {
                var outboxMessage = new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    EventType = "PaymentSucceeded",
                    Payload = System.Text.Json.JsonSerializer.Serialize(new { OrderId = payment.OrderId }),
                    CreatedAt = DateTime.UtcNow,
                    Processed = false,
                    RetryCount = 0
                };
                await _outboxRepository.AddAsync(outboxMessage);
            }

            await _unitOfWork.SaveChangesAsync();
            return MapToDto(payment);
        }

        public async Task<PaymentDto> GetOrderPaymentAsync(Guid userId, Guid orderId)
        {
            bool ownsOrder = await _orderServiceClient.VerifyOrderOwnershipAsync(orderId, userId);
            if (!ownsOrder)
                throw new InvalidOperationException("Order not found or access denied.");

            var payment = await _paymentRepository.GetByOrderIdAsync(orderId);
            if (payment == null)
                return null;

            return MapToDto(payment);
        }

        public async Task<PaymentDto?> GetInternalOrderPaymentAsync(Guid orderId)
        {
            var payment = await _paymentRepository.GetByOrderIdAsync(orderId);
            if (payment == null)
                return null;

            return MapToDto(payment);
        }

        public async Task<PaymentDto> InitiateRefundAsync(Guid orderId)
        {
            var payment = await _paymentRepository.GetByOrderIdAsync(orderId);
            if (payment == null)
                throw new InvalidOperationException("Payment not found.");

            if (payment.Status == "RefundInProcess" || payment.Status == "RefundCompleted" || payment.Status == "Refunded")
                return MapToDto(payment);

            if (payment.Status != "Success")
                throw new InvalidOperationException("Only successful payments can be refunded.");

            payment.Status = "RefundInProcess";
            payment.ProcessedAt = DateTime.UtcNow;

            await _paymentRepository.UpdateAsync(payment);
            await _unitOfWork.SaveChangesAsync();

            return MapToDto(payment);
        }

        public async Task<PaymentDto> CompleteRefundAsync(Guid adminUserId, Guid paymentId, string idempotencyKey, string requestPath)
        {
            var idempotencyRecord = await _idempotencyRepository.GetAsync(adminUserId, idempotencyKey, requestPath);
            if (idempotencyRecord != null)
            {
                if (idempotencyRecord.Succeeded && idempotencyRecord.OrderId.HasValue)
                {
                    var existingPayment = await _paymentRepository.GetByIdAsync(paymentId);
                    return MapToDto(existingPayment);
                }
                throw new InvalidOperationException("Previous request with same idempotency key is still processing or failed.");
            }

            idempotencyRecord = new IdempotencyRecord
            {
                Id = Guid.NewGuid(),
                UserId = adminUserId,
                Key = idempotencyKey,
                RequestPath = requestPath,
                CreatedAt = DateTime.UtcNow,
                Succeeded = false
            };
            await _idempotencyRepository.AddAsync(idempotencyRecord);
            await _unitOfWork.SaveChangesAsync();

            var payment = await _paymentRepository.GetByIdAsync(paymentId);
            if (payment == null)
                throw new InvalidOperationException("Payment not found.");

            if (payment.Status == "RefundCompleted" || payment.Status == "Refunded")
            {
                idempotencyRecord.OrderId = payment.OrderId;
                idempotencyRecord.Succeeded = true;
                await _idempotencyRepository.UpdateAsync(idempotencyRecord);
                await _unitOfWork.SaveChangesAsync();
                return MapToDto(payment);
            }

            if (payment.Status != "RefundInProcess")
                throw new InvalidOperationException("Payment must be in RefundInProcess state to be completed.");

            var result = await _paymentProvider.RefundPaymentAsync(payment.TransactionId ?? "", payment.Amount);
            if (!result.IsSuccess)
                throw new InvalidOperationException(result.ErrorMessage ?? "Refund failed at provider.");

            payment.Status = "RefundCompleted";
            payment.ProcessedAt = DateTime.UtcNow;

            await _paymentRepository.UpdateAsync(payment);

            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                EventType = "PaymentRefundCompleted",
                Payload = System.Text.Json.JsonSerializer.Serialize(new { OrderId = payment.OrderId }),
                CreatedAt = DateTime.UtcNow,
                Processed = false,
                RetryCount = 0
            };
            await _outboxRepository.AddAsync(outboxMessage);

            idempotencyRecord.OrderId = payment.OrderId;
            idempotencyRecord.Succeeded = true;
            await _idempotencyRepository.UpdateAsync(idempotencyRecord);

            await _unitOfWork.SaveChangesAsync();

            return MapToDto(payment);
        }

        private PaymentDto MapToDto(Payment payment)
        {
            int statusInt = 0;
            switch(payment.Status)
            {
                case "Pending": statusInt = 0; break;
                case "Success": statusInt = 1; break;
                case "Failed": statusInt = 2; break;
                case "RefundInProcess": statusInt = 3; break;
                case "RefundCompleted": 
                case "Refunded": statusInt = 4; break;
            }

            return new PaymentDto
            {
                Id = payment.Id,
                OrderId = payment.OrderId,
                Amount = payment.Amount,
                Status = statusInt,
                Method = payment.Method,
                TransactionId = payment.TransactionId,
                ProcessedAt = payment.ProcessedAt
            };
        }
    }
}
