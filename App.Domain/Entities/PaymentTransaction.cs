using App.Domain.Interceptors;

namespace App.Domain.Entities
{
    public class PaymentTransaction : ISoftDeletable
    {
        public Guid Id { get; set; }
        public long PaymobTransactionId { get; set; }
        public long PaymobOrderId { get; set; }
        public Guid? OrderId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "EGP";
        public string Status { get; set; } = "Pending";
        public string PaymentMethod { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty;
        public bool Is3DSecure { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ResponseCode { get; set; }
        public decimal? RefundedAmount { get; set; }
        public bool IsSettled { get; set; }
        public decimal? MerchantCommission { get; set; }
        public DateTime PaymobCreatedAt { get; set; }
        public string RawPayload { get; set; } = string.Empty;
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime DeletedAt { get; set; }

        public Order? Order { get; set; }
    }
}
