using App.Application.Payments.Dto.Paymob;

namespace App.Application.Common.Interfaces.Services
{
    public interface IPaymobService
    {
        Task<PaymobIntentionResponse> CreateIntentionAsync(PaymobIntentionRequest request, CancellationToken cancellationToken = default);

        string BuildCheckoutUrl(string clientSecret);

        bool VerifyTransactionHmac(PaymobTransactionDto transaction, string receivedHmac);

        Task<PaymobOperationResult> RefundTransactionAsync(long transactionId, long amountCents, CancellationToken cancellationToken = default);

        Task<PaymobOperationResult> VoidTransactionAsync(long transactionId, CancellationToken cancellationToken = default);
    }
}
