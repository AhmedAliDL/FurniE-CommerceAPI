using FluentValidation;

namespace App.Application.Payments.Command.CreatePaymentSession
{
    public class CreatePaymentSessionValidator : AbstractValidator<CreatePaymentSessionCommand>
    {
        public CreatePaymentSessionValidator()
        {
            RuleFor(c => c.OrderId)
                .NotEmpty()
                .WithMessage("Order id is required.");

            RuleFor(c => c.IdempotencyKey)
                .MaximumLength(100)
                .WithMessage("Idempotency key must be at most 100 characters.");
        }
    }
}
