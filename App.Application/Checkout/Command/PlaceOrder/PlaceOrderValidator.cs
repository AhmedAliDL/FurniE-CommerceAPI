using FluentValidation;

namespace App.Application.Checkout.Command.PlaceOrder
{
    public class PlaceOrderValidator : AbstractValidator<PlaceOrderCommand>
    {
        public PlaceOrderValidator()
        {
            RuleFor(o => o.PaymentMethod)
                .NotEmpty()
                .WithMessage("Payment method is required.");
            RuleFor(o => o.Address)
                .MaximumLength(100)
                .WithMessage("Address must not exceed 100 characters.");
            RuleFor(o => o.City)
                .MaximumLength(80)
                .WithMessage("City must not exceed 80 characters.");
            RuleFor(o => o.CardNumber)
                .Length(16)
                .When(o => !string.IsNullOrEmpty(o.CardNumber))
                .WithMessage("Card number must be 16 digits.");
            RuleFor(o => o.ExpirationMonth)
                .InclusiveBetween(1, 12)
                .WithMessage("Expiration month must be between 1 and 12.");
            RuleFor(o => o.ExpirationYear)
                .MaximumLength(2)
                .WithMessage("Expiration year must not exceed 2 characters.");
            RuleFor(o => o.CVV)
                .MaximumLength(3)
                .WithMessage("CVV must not exceed 3 characters.");
            RuleFor(o => o.IdempotencyKey)
                .MaximumLength(100)
                .When(o => !string.IsNullOrEmpty(o.IdempotencyKey))
                .WithMessage("Idempotency key must not exceed 100 characters.");
            RuleFor(o => o.CouponCode)
                .MaximumLength(50)
                .When(o => !string.IsNullOrEmpty(o.CouponCode))
                .WithMessage("Coupon code must not exceed 50 characters.");
        }
    }
}
