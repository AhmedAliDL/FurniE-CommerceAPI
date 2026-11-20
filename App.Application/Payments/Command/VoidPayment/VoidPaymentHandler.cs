using System.Text.Json;
using App.Application.Common.Exceptions;
using App.Application.Common.Idempotency;
using App.Application.Common.Integration;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Payments.Dto;
using App.Application.Responses;
using App.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Application.Payments.Command.VoidPayment
{
    public class VoidPaymentHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IPaymobService paymobService, ILogger<VoidPaymentHandler> logger) : IRequestHandler<VoidPaymentCommand, ResponseResult<PaymentOperationResultDto>>
    {
        private static readonly TimeSpan VoidWindow = TimeSpan.FromHours(24);

        public async Task<ResponseResult<PaymentOperationResultDto>> Handle(VoidPaymentCommand request, CancellationToken cancellationToken)
        {
            Guid adminId = currentUserService.UserId ?? throw new NotFoundException("User not found.");

            // Replaying a completed void must return its stored result and never reach Paymob twice.
            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                var processed = await FindRecordAsync(adminId, request.IdempotencyKey, cancellationToken);
                if (processed != null)
                {
                    var replay = Deserialize(processed.ResponsePayload);
                    if (replay is not null)
                    {
                        logger.LogInformation("Duplicate void request for idempotency key {IdempotencyKey}. Returning stored result.", request.IdempotencyKey);
                        return ResponseResult<PaymentOperationResultDto>.Success(replay);
                    }

                    return ResponseResult<PaymentOperationResultDto>.Failure("This idempotency key is already in use for another void request.");
                }
            }

            var order = await unitOfWork.Orders.FindAsync(o => o.OrderId == request.OrderId, cancellationToken)
                ?? throw new NotFoundException($"Order with id {request.OrderId} was not found.");

            var transactions = await unitOfWork.PaymentTransactions.FindAllAsync(t => t.OrderId == order.OrderId, cancellationToken);
            var transaction = transactions
                .Where(t => t.Status == "Success")
                .OrderByDescending(t => t.PaymobCreatedAt)
                .FirstOrDefault();
            if (transaction == null)
                return ResponseResult<PaymentOperationResultDto>.Failure("No successful payment found for this order.");

            // Void only within 24 hours of payment; Paymob itself rejects voids once the transaction settles.
            if (DateTime.UtcNow - transaction.PaymobCreatedAt > VoidWindow)
            {
                return ResponseResult<PaymentOperationResultDto>.Failure("Void is only allowed within 24 hours of payment.");
            }

            var result = await paymobService.VoidTransactionAsync(transaction.PaymobTransactionId, cancellationToken);
            if (!result.Success)
                return ResponseResult<PaymentOperationResultDto>.Failure(result.Message ?? "Paymob rejected the void.");

            transaction.Status = "Voided";
            transaction.UpdatedAt = DateTime.UtcNow;
            unitOfWork.PaymentTransactions.Update(transaction);

            var payment = await unitOfWork.Payments.FindAsync(p => p.OrderId == order.OrderId, cancellationToken);
            if (payment != null)
            {
                payment.PaymentStatus = "Voided";
                payment.UpdatedAt = DateTime.UtcNow;
                unitOfWork.Payments.Update(payment);
            }

            var operationResult = new PaymentOperationResultDto
            {
                PaymobTransactionId = transaction.PaymobTransactionId,
                Status = transaction.Status,
                RefundedAmount = transaction.RefundedAmount ?? 0m,
                Message = result.Message
            };

            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                await unitOfWork.IdempotencyRecords.AddAsync(new IdempotencyRecord
                {
                    UserId = adminId,
                    Operation = IdempotencyOperations.VoidPayment,
                    IdempotencyKey = request.IdempotencyKey,
                    OrderId = order.OrderId,
                    ResponsePayload = JsonSerializer.Serialize(operationResult)
                }, cancellationToken);
            }

            await IntegrationEvents.PublishAsync(unitOfWork, IntegrationEvents.Voided, new
            {
                orderId = order.OrderId,
                transactionId = transaction.PaymobTransactionId,
                status = transaction.Status
            }, cancellationToken);

            try
            {
                await unitOfWork.CompleteAsync(cancellationToken);
            }
            catch (DbUpdateException) when (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                var winner = await FindRecordAsync(adminId, request.IdempotencyKey, cancellationToken);
                if (winner is not null && Deserialize(winner.ResponsePayload) is { } concurrentReplay)
                {
                    logger.LogInformation("Concurrent duplicate void detected for idempotency key {IdempotencyKey}. Returning stored result.", request.IdempotencyKey);
                    return ResponseResult<PaymentOperationResultDto>.Success(concurrentReplay);
                }

                throw;
            }

            logger.LogInformation("Order {OrderId} payment voided. TransactionId {TransactionId}",
                order.OrderId, transaction.PaymobTransactionId);
            return ResponseResult<PaymentOperationResultDto>.Success(operationResult);
        }

        private Task<IdempotencyRecord?> FindRecordAsync(Guid userId, string idempotencyKey, CancellationToken cancellationToken) =>
            unitOfWork.IdempotencyRecords.FindAsync(
                r => r.UserId == userId && r.Operation == IdempotencyOperations.VoidPayment && r.IdempotencyKey == idempotencyKey,
                cancellationToken);

        private static PaymentOperationResultDto? Deserialize(string? payload) =>
            string.IsNullOrWhiteSpace(payload) ? null : JsonSerializer.Deserialize<PaymentOperationResultDto>(payload);
    }
}
