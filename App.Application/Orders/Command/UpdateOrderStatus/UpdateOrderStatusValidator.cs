using App.Domain.Enums;
using FluentValidation;

namespace App.Application.Orders.Command.UpdateOrderStatus
{
    public class UpdateOrderStatusValidator : AbstractValidator<UpdateOrderStatusCommand>
    {
        public UpdateOrderStatusValidator()
        {
            RuleFor(x => x.OrderId)
                .NotEmpty();

            RuleFor(x => x.Status)
                .IsInEnum();
        }
    }
}
