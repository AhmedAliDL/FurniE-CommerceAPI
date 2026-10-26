using App.Domain.Enums;
using App.Domain.Interceptors;

namespace App.Domain.Entities
{
    public class Order : ISoftDeletable
    {
        public Guid OrderId { get; set; }
        public Guid UserId { get; set; }
        public decimal TotalPrice { get; set; }
        public OrderStatus OrderStatus { get; set; } = OrderStatus.Pending;
        public string ShippingAddress { get; set; } = string.Empty;
        public decimal CouponDiscount { get; set; } = 0m;
        public decimal ShippingFee { get; set; } = 0m;
        public DateTime? ReviewReminderSentAt { get; set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime DeletedAt { get; set; }

        public AppUser Customer { get; set; } = null!;
        public Payment Payment { get; set; } = null!;

        public ICollection<OrderItem> OrderItems { get; set; } = null!;

        public ICollection<PaymentTransaction> PaymentTransactions { get; set; } = null!;
        public ICollection<IdempotencyRecord> IdempotencyRecords { get; set; } = null!;

        public ICollection<CancellationRequest> CancellationRequests { get; set; } = null!;
        public ICollection<ReturnRequest> ReturnRequests { get; set; } = null!;



    }

}
