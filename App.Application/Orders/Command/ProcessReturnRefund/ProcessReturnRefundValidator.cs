using FluentValidation;

namespace App.Application.Orders.Command.ProcessReturnRefund
{
    public class ProcessReturnRefundValidator : AbstractValidator<ProcessReturnRefundCommand>
    {
        public ProcessReturnRefundValidator()
        {
            RuleFor(c => c.RequestId)
                .NotEmpty()
                .WithMessage("Request id is required.");

            RuleFor(c => c.AmountCents)
                .GreaterThan(0)
                .When(c => c.AmountCents.HasValue)
                .WithMessage("Refund amount must be greater than zero.");
        }
    }
}
