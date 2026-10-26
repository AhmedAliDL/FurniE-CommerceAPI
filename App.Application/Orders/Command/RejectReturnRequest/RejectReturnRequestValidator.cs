using FluentValidation;

namespace App.Application.Orders.Command.RejectReturnRequest
{
    public class RejectReturnRequestValidator : AbstractValidator<RejectReturnRequestCommand>
    {
        public RejectReturnRequestValidator()
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
