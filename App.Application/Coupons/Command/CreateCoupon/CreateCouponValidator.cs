using FluentValidation;

namespace App.Application.Coupons.Command.CreateCoupon
{
    public class CreateCouponValidator : AbstractValidator<CreateCouponCommand>
    {
        public CreateCouponValidator()
        {
            RuleFor(c => c.CouponCode)
                .NotEmpty()
                .WithMessage("Coupon code is required.");
            RuleFor(c => (double)c.CouponRatio)
                .InclusiveBetween(0.1, 100)
                .WithMessage("Coupon ratio must be between 0.1 and 100.");
        }
    }
}
