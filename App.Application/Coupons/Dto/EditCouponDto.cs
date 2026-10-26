namespace App.Application.Coupons.Dto
{
    public class EditCouponDto
    {
        public Guid CouponId { get; set; }
        public string? CouponCode { get; set; }
        public decimal? CouponRatio { get; set; }
        public int? PeriodInDays { get; set; }
        public int? PeriodInHours { get; set; }
        public int? PeriodInMinutes { get; set; }
    }
}
