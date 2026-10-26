using System.Text.Json;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Payments.Dto.Paymob;
using App.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Payments.Command.HandlePaymobCallback
{
    public class HandlePaymobCallbackHandler(IUnitOfWork unitOfWork, IPaymobService paymobService, ILogger<HandlePaymobCallbackHandler> logger) : IRequestHandler<HandlePaymobCallbackCommand, bool>
    {
        public async Task<bool> Handle(HandlePaymobCallbackCommand request, CancellationToken cancellationToken)
        {
            // Never throw here: Paymob marks the webhook failed and retries on any non-2xx,
            // so malformed payloads and bad signatures are logged and acknowledged instead.
            var transaction = request.Body.Obj;
            if (transaction == null)
            {
                logger.LogWarning("Paymob callback ignored: payload had no transaction object.");
                return false;
            }

            if (!paymobService.VerifyTransactionHmac(transaction, request.Hmac ?? string.Empty))
            {
                logger.LogWarning("Paymob callback rejected: HMAC verification failed. TransactionId {TransactionId}",
                    transaction.Id);
                return false;
            }

            logger.LogInformation("Paymob callback received. TransactionId {TransactionId} Success {Success}",
                transaction.Id, transaction.Success);

            // Paymob retries webhooks; never process the same transaction twice.
            var alreadyRecorded = await unitOfWork.PaymentTransactions.FindAsync(
                t => t.PaymobTransactionId == transaction.Id,
                cancellationToken);
            if (alreadyRecorded != null)
            {
                logger.LogInformation("Duplicate Paymob callback ignored. TransactionId {TransactionId}", transaction.Id);
                return false;
            }

            Guid? orderId = Guid.TryParse(transaction.Order?.MerchantOrderId, out var parsedOrderId)
                ? parsedOrderId
                : null;

            var paymentTransaction = new PaymentTransaction
            {
                PaymobTransactionId = transaction.Id,
                PaymobOrderId = transaction.Order?.Id ?? 0,
                OrderId = orderId,
                Amount = transaction.AmountCents / 100m,
                Currency = transaction.Currency ?? "EGP",
                Status = MapTransactionStatus(transaction),
                PaymentMethod = transaction.SourceData?.SubType ?? "Unknown",
                SourceType = transaction.SourceData?.Type ?? "Unknown",
                Is3DSecure = transaction.Is3DSecure,
                ErrorMessage = transaction.Data?.Message,
                ResponseCode = transaction.Data?.TxnResponseCode,
                RefundedAmount = transaction.RefundedAmountCents / 100m,
                IsSettled = transaction.IsSettled,
                MerchantCommission = transaction.MerchantCommission,
                PaymobCreatedAt = DateTime.TryParse(
                    transaction.CreatedAt,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.RoundtripKind,
                    out var paymobCreatedAt)
                    ? paymobCreatedAt
                    : DateTime.UtcNow,
                RawPayload = JsonSerializer.Serialize(request.Body)
            };
            await unitOfWork.PaymentTransactions.AddAsync(paymentTransaction, cancellationToken);

            if (orderId != null)
            {
                var payment = await unitOfWork.Payments.FindAsync(p => p.OrderId == orderId, cancellationToken);
                if (payment != null)
                {
                    payment.PaymentStatus = MapPaymentStatus(transaction);
                    payment.PaymentMethod = transaction.SourceData?.SubType ?? payment.PaymentMethod;
                    payment.UpdatedAt = DateTime.UtcNow;
                    unitOfWork.Payments.Update(payment);
                }
                else
                {
                    logger.LogWarning(
                        "Paymob callback has no matching local payment row. OrderId {OrderId} TransactionId {TransactionId}",
                        orderId, transaction.Id);
                }
            }
            else
            {
                logger.LogWarning(
                    "Paymob callback could not be matched to an order. TransactionId {TransactionId}",
                    transaction.Id);
            }

            await unitOfWork.CompleteAsync(cancellationToken);

            logger.LogInformation("Paymob callback processed. TransactionId {TransactionId}", transaction.Id);
            return true;
        }

        private static string MapTransactionStatus(PaymobTransactionDto transaction) => transaction.Success switch
        {
            true when transaction.IsRefunded => "Refunded",
            true => "Success",
            _ when transaction.IsVoided => "Voided",
            _ when transaction.Pending => "Pending",
            _ => "Failed"
        };

        private static string MapPaymentStatus(PaymobTransactionDto transaction) => transaction.Success switch
        {
            true when transaction.IsRefunded => "Refunded",
            true => "Completed",
            _ when transaction.Pending => "Pending",
            _ => "Failed"
        };
    }
}
