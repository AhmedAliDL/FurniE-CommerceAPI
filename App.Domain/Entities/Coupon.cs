using App.Domain.Interceptors;

namespace App.Domain.Entities
{
    public class Coupon : ISoftDeletable
    {
        public Guid CouponId { get; set; }
        public string CouponCode { get; set; } = string.Empty;
        public decimal CouponRatio { get; set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime DeletedAt { get; set; }

        public DateTime ExpireDate { get; set; }
    }
}
