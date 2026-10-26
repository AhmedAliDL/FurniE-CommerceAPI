using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Payments.Dto;
using App.Application.Responses;
using App.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Payments.Command.RefundPayment
{
    public class RefundPaymentHandler(IUnitOfWork unitOfWork, IPaymobService paymobService, ILogger<RefundPaymentHandler> logger) : IRequestHandler<RefundPaymentCommand, ResponseResult<PaymentOperationResultDto>>
    {
        public async Task<ResponseResult<PaymentOperationResultDto>> Handle(RefundPaymentCommand request, CancellationToken cancellationToken)
        {
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

            await unitOfWork.CompleteAsync(cancellationToken);

            logger.LogInformation("Order {OrderId} refunded. TransactionId {TransactionId} RefundCents {RefundCents}",
                order.OrderId, transaction.PaymobTransactionId, refundCents);
            return ResponseResult<PaymentOperationResultDto>.Success(new PaymentOperationResultDto
            {
                PaymobTransactionId = transaction.PaymobTransactionId,
                Status = transaction.Status,
                RefundedAmount = transaction.RefundedAmount ?? 0m,
                Message = result.Message
            });
        }
    }
}
