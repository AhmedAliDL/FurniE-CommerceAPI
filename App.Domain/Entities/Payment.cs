using App.Domain.Interceptors;

namespace App.Domain.Entities
{
    public class Payment : ISoftDeletable
    {
        public Guid PaymentId { get; set; }
        public Guid? OrderId { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public double Amount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime DeletedAt { get; set; }

        public Order Order { get; set; } = null!;
    }

}
