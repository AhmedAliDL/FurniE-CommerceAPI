using App.Domain.Interceptors;

namespace App.Domain.Entities
{
    public class ReturnItem : ISoftDeletable
    {
        public Guid Id { get; set; }
        public Guid ReturnRequestId { get; set; }
        public Guid OrderItemId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime DeletedAt { get; set; }

        public ReturnRequest ReturnRequest { get; set; } = null!;
        public OrderItem OrderItem { get; set; } = null!;
    }
}
