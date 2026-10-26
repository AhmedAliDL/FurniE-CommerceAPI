using App.Domain.Enums;
using App.Domain.Interceptors;

namespace App.Domain.Entities
{
    public class CancellationRequest : ISoftDeletable
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public Guid CustomerId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string PreviousOrderStatus { get; set; } = string.Empty;
        public CancellationRequestStatus Status { get; set; } = CancellationRequestStatus.Pending;
        public string? AdminNote { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime DeletedAt { get; set; }

        public Order Order { get; set; } = null!;
        public AppUser Customer { get; set; } = null!;
    }
}
