using FluentValidation;

namespace App.Application.Coupons.Command.UpdateCoupon
{
    public class UpdateCouponValidator : AbstractValidator<UpdateCouponCommand>
    {
        public UpdateCouponValidator()
        {
            RuleFor(c => c.CouponId)
                .NotEmpty()
                .WithMessage("Coupon id is required.");
            RuleFor(c => (double)c.CouponRatio!)
                .InclusiveBetween(0.1, 100)
                .When(c => c.CouponRatio.HasValue)
                .WithMessage("Coupon ratio must be between 0.1 and 100.");
        }
    }
}
