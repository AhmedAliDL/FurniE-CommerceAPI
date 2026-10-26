using MediatR;

namespace App.Application.Coupons.Command.CreateCoupon
{
    public record CreateCouponCommand : IRequest<Guid>
    {
        public string CouponCode { get; set; } = null!;
        public decimal CouponRatio { get; set; }
        public int PeriodInDays { get; set; }
        public int PeriodInHours { get; set; }
        public int PeriodInMinutes { get; set; }
    }
}
