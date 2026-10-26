using FluentValidation;

namespace App.Application.Payments.Command.VoidPayment
{
    public class VoidPaymentValidator : AbstractValidator<VoidPaymentCommand>
    {
        public VoidPaymentValidator()
        {
            RuleFor(c => c.OrderId)
                .NotEmpty()
                .WithMessage("Order id is required.");
        }
    }
}
