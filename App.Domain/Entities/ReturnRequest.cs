using App.Domain.Enums;
using App.Domain.Interceptors;

namespace App.Domain.Entities
{
    public class ReturnRequest : ISoftDeletable
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public Guid CustomerId { get; set; }
        public ReturnReasonCategory ReasonCategory { get; set; } = ReturnReasonCategory.CustomerChoice;
        public string ReasonText { get; set; } = string.Empty;
        public ReturnRequestStatus Status { get; set; } = ReturnRequestStatus.Pending;
        public string? CourierName { get; set; }
        public string? CourierPhone { get; set; }
        public DateTime? ReceivedAt { get; set; }
        public long? RefundAmountCents { get; set; }
        public string? AdminNote { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime DeletedAt { get; set; }

        public Order Order { get; set; } = null!;
        public AppUser Customer { get; set; } = null!;
        public ICollection<ReturnItem> ReturnItems { get; set; } = null!;
    }
}
