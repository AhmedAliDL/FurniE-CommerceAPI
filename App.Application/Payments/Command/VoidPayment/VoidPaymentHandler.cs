using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Payments.Dto;
using App.Application.Responses;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Payments.Command.VoidPayment
{
    public class VoidPaymentHandler(IUnitOfWork unitOfWork, IPaymobService paymobService, ILogger<VoidPaymentHandler> logger) : IRequestHandler<VoidPaymentCommand, ResponseResult<PaymentOperationResultDto>>
    {
        private static readonly TimeSpan VoidWindow = TimeSpan.FromHours(24);

        public async Task<ResponseResult<PaymentOperationResultDto>> Handle(VoidPaymentCommand request, CancellationToken cancellationToken)
        {
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

            await unitOfWork.CompleteAsync(cancellationToken);

            logger.LogInformation("Order {OrderId} payment voided. TransactionId {TransactionId}",
                order.OrderId, transaction.PaymobTransactionId);
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
