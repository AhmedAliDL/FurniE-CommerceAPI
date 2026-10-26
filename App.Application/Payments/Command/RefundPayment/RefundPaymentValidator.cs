using FluentValidation;

namespace App.Application.Payments.Command.RefundPayment
{
    public class RefundPaymentValidator : AbstractValidator<RefundPaymentCommand>
    {
        public RefundPaymentValidator()
        {
            RuleFor(c => c.OrderId)
                .NotEmpty()
                .WithMessage("Order id is required.");

            RuleFor(c => c.AmountCents)
                .GreaterThan(0)
                .When(c => c.AmountCents.HasValue)
                .WithMessage("Refund amount must be greater than zero.");
        }
    }
}
