using App.Domain.Enums;
using FluentValidation;

namespace App.Application.Orders.Command.RequestOrderReturn
{
    public class RequestOrderReturnValidator : AbstractValidator<RequestOrderReturnCommand>
    {
        public RequestOrderReturnValidator()
        {
            RuleFor(c => c.OrderId)
                .NotEmpty()
                .WithMessage("Order id is required.");

            RuleFor(c => c.ReasonCategory)
                .NotEmpty()
                .Must(c => string.Equals(c, ReturnReasonCategory.MerchantDefect.ToString(), StringComparison.OrdinalIgnoreCase)
                    || string.Equals(c, ReturnReasonCategory.CustomerChoice.ToString(), StringComparison.OrdinalIgnoreCase))
                .WithMessage($"Reason category must be '{ReturnReasonCategory.MerchantDefect}' or '{ReturnReasonCategory.CustomerChoice}'.");

            RuleFor(c => c.ReasonText)
                .NotEmpty()
                .MaximumLength(500)
                .WithMessage("A return reason is required (max 500 characters).");

            RuleFor(c => c.Items)
                .NotEmpty()
                .WithMessage("At least one item must be selected for return.");

            RuleForEach(c => c.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.OrderItemId)
                    .NotEmpty()
                    .WithMessage("Order item id is required.");

                item.RuleFor(i => i.Quantity)
                    .GreaterThan(0)
                    .WithMessage("Return quantity must be greater than zero.");
            });
        }
    }
}
