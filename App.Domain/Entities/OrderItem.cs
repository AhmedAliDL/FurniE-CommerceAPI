using App.Domain.Interceptors;

namespace App.Domain.Entities
{
    public class OrderItem : ISoftDeletable
    {
        public Guid OrderItemId { get; set; }
        public Guid OrderId { get; set; }
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime DeletedAt { get; set; }

        public Order Order { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }

}
