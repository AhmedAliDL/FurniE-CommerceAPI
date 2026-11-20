namespace App.Application.Common.Idempotency
{
    public static class IdempotencyOperations
    {
        public const string Checkout = "checkout";
        public const string CreatePaymentSession = "create-payment-session";
        public const string RefundPayment = "refund-payment";
        public const string VoidPayment = "void-payment";
        public const string ApproveCancellationRequest = "approve-cancellation-request";
        public const string ProcessReturnRefund = "process-return-refund";
    }
}
