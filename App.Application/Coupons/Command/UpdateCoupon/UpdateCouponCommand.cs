using MediatR;

namespace App.Application.Coupons.Command.UpdateCoupon
{
    public record UpdateCouponCommand : IRequest<bool>
    {
        public Guid CouponId { get; set; }
        public string? CouponCode { get; set; }
        public decimal? CouponRatio { get; set; }
        public int? PeriodInDays { get; set; }
        public int? PeriodInHours { get; set; }
        public int? PeriodInMinutes { get; set; }
    }
}
