using FluentValidation;

namespace App.Application.Orders.Command.RejectCancellationRequest
{
    public class RejectCancellationRequestValidator : AbstractValidator<RejectCancellationRequestCommand>
    {
        public RejectCancellationRequestValidator()
        {
            RuleFor(c => c.RequestId)
                .NotEmpty()
                .WithMessage("Request id is required.");

            RuleFor(c => c.AdminNote)
                .NotEmpty()
                .MaximumLength(500)
                .WithMessage("A rejection note is required (max 500 characters).");
        }
    }
}
