using System.Text.Json;
using App.Application.Common.Exceptions;
using App.Application.Common.Integration;
using App.Application.Common.Idempotency;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Payments.Dto;
using App.Application.Responses;
using App.Domain.Entities;
using App.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Application.Payments.Command.RefundPayment
{
    public class RefundPaymentHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IPaymobService paymobService, ILogger<RefundPaymentHandler> logger) : IRequestHandler<RefundPaymentCommand, ResponseResult<PaymentOperationResultDto>>
    {
        public async Task<ResponseResult<PaymentOperationResultDto>> Handle(RefundPaymentCommand request, CancellationToken cancellationToken)
        {
            Guid adminId = currentUserService.UserId ?? throw new NotFoundException("User not found.");

            // Replaying a completed refund must return its stored result and never reach Paymob twice.
            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                var processed = await FindRecordAsync(adminId, request.IdempotencyKey, cancellationToken);
                if (processed != null)
                {
                    var replay = Deserialize(processed.ResponsePayload);
                    if (replay is not null)
                    {
                        logger.LogInformation("Duplicate refund request for idempotency key {IdempotencyKey}. Returning stored result.", request.IdempotencyKey);
                        return ResponseResult<PaymentOperationResultDto>.Success(replay);
                    }

                    return ResponseResult<PaymentOperationResultDto>.Failure("This idempotency key is already in use for another refund request.");
                }
            }

            var order = await unitOfWork.Orders.FindAsync(o => o.OrderId == request.OrderId, cancellationToken)
                ?? throw new NotFoundException($"Order with id {request.OrderId} was not found.");

            // Delivery time is tracked as UpdatedAt at the moment the order reached Delivered;
            // the 14-day refund window is measured from it.
            if (order.OrderStatus != OrderStatus.Delivered
                || order.UpdatedAt == null
                || order.UpdatedAt < DateTime.UtcNow.AddDays(-14))
            {
                return ResponseResult<PaymentOperationResultDto>.Failure("Refunds are only allowed within 14 days after delivery.");
            }

            var transactions = await unitOfWork.PaymentTransactions.FindAllAsync(t => t.OrderId == order.OrderId, cancellationToken);
            var transaction = transactions
                .Where(t => t.Status is "Success" or "Refunded")
                .OrderByDescending(t => t.PaymobCreatedAt)
                .FirstOrDefault();
            if (transaction == null)
                return ResponseResult<PaymentOperationResultDto>.Failure("No successful payment found for this order.");

            var amountCents = (long)Math.Round(transaction.Amount * 100m);
            var refundedCents = (long)Math.Round((transaction.RefundedAmount ?? 0m) * 100m);
            var remainingCents = amountCents - refundedCents;
            var refundCents = request.AmountCents ?? remainingCents;
            if (refundCents <= 0 || refundCents > remainingCents)
            {
                return ResponseResult<PaymentOperationResultDto>.Failure(
                    $"Refund amount must be between 1 and {remainingCents} cents.");
            }

            var result = await paymobService.RefundTransactionAsync(transaction.PaymobTransactionId, refundCents, cancellationToken);
            if (!result.Success)
                return ResponseResult<PaymentOperationResultDto>.Failure(result.Message ?? "Paymob rejected the refund.");

            var newRefundedCents = refundedCents + refundCents;
            transaction.RefundedAmount = newRefundedCents / 100m;
            if (newRefundedCents >= amountCents)
                transaction.Status = "Refunded";
            transaction.UpdatedAt = DateTime.UtcNow;
            unitOfWork.PaymentTransactions.Update(transaction);

            var payment = await unitOfWork.Payments.FindAsync(p => p.OrderId == order.OrderId, cancellationToken);
            if (payment != null)
            {
                payment.PaymentStatus = transaction.Status;
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
                    Operation = IdempotencyOperations.RefundPayment,
                    IdempotencyKey = request.IdempotencyKey,
                    OrderId = order.OrderId,
                    ResponsePayload = JsonSerializer.Serialize(operationResult)
                }, cancellationToken);
            }

            await IntegrationEvents.PublishAsync(unitOfWork, IntegrationEvents.Refunded, new
            {
                orderId = order.OrderId,
                transactionId = transaction.PaymobTransactionId,
                refundCents,
                refundedAmount = transaction.RefundedAmount ?? 0m,
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
                    logger.LogInformation("Concurrent duplicate refund detected for idempotency key {IdempotencyKey}. Returning stored result.", request.IdempotencyKey);
                    return ResponseResult<PaymentOperationResultDto>.Success(concurrentReplay);
                }

                throw;
            }

            logger.LogInformation("Order {OrderId} refunded. TransactionId {TransactionId} RefundCents {RefundCents}",
                order.OrderId, transaction.PaymobTransactionId, refundCents);
            return ResponseResult<PaymentOperationResultDto>.Success(operationResult);
        }

        private Task<IdempotencyRecord?> FindRecordAsync(Guid userId, string idempotencyKey, CancellationToken cancellationToken) =>
            unitOfWork.IdempotencyRecords.FindAsync(
                r => r.UserId == userId && r.Operation == IdempotencyOperations.RefundPayment && r.IdempotencyKey == idempotencyKey,
                cancellationToken);

        private static PaymentOperationResultDto? Deserialize(string? payload) =>
            string.IsNullOrWhiteSpace(payload) ? null : JsonSerializer.Deserialize<PaymentOperationResultDto>(payload);
    }
}
