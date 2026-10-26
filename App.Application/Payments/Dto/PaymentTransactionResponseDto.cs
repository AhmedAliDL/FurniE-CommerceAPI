namespace App.Application.Payments.Dto
{
    // Admin-facing projection of a Paymob transaction; RawPayload is deliberately excluded.
    public class PaymentTransactionResponseDto
    {
        public Guid Id { get; set; }
        public long PaymobTransactionId { get; set; }
        public long PaymobOrderId { get; set; }
        public Guid? OrderId { get; set; }
        public string? CustomerName { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty;
        public bool Is3DSecure { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ResponseCode { get; set; }
        public decimal? RefundedAmount { get; set; }
        public bool IsSettled { get; set; }
        public decimal? MerchantCommission { get; set; }
        public DateTime PaymobCreatedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
