namespace App.Application.Coupons.Dto
{
    public class CouponDto
    {
        public Guid CouponId { get; set; }
        public string CouponCode { get; set; } = string.Empty;
        public decimal CouponRatio { get; set; }
        public DateTime ExpireDate { get; set; }
    }
}
