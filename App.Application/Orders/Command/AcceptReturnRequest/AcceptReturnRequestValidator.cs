using FluentValidation;

namespace App.Application.Orders.Command.AcceptReturnRequest
{
    public class AcceptReturnRequestValidator : AbstractValidator<AcceptReturnRequestCommand>
    {
        public AcceptReturnRequestValidator()
        {
            RuleFor(c => c.RequestId)
                .NotEmpty()
                .WithMessage("Request id is required.");

            RuleFor(c => c.CourierName)
                .NotEmpty()
                .MaximumLength(100)
                .WithMessage("Courier name is required (max 100 characters).");

            RuleFor(c => c.CourierPhone)
                .NotEmpty()
                .MaximumLength(20)
                .WithMessage("Courier phone is required (max 20 characters).");
        }
    }
}
