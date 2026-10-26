using FluentValidation;

namespace App.Application.Orders.Command.CancelOrder
{
    public class CancelOrderValidator : AbstractValidator<CancelOrderCommand>
    {
        public CancelOrderValidator()
        {
            RuleFor(x => x.OrderId)
                .NotEmpty();
        }
    }
}
