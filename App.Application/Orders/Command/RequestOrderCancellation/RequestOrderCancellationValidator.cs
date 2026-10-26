using FluentValidation;

namespace App.Application.Orders.Command.RequestOrderCancellation
{
    public class RequestOrderCancellationValidator : AbstractValidator<RequestOrderCancellationCommand>
    {
        public RequestOrderCancellationValidator()
        {
            RuleFor(c => c.OrderId)
                .NotEmpty()
                .WithMessage("Order id is required.");

            RuleFor(c => c.Reason)
                .NotEmpty()
                .MaximumLength(500)
                .WithMessage("A cancellation reason is required (max 500 characters).");
        }
    }
}
