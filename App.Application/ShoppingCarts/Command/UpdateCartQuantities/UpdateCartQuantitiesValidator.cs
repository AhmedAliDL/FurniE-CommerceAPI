using FluentValidation;

namespace App.Application.ShoppingCarts.Command.UpdateCartQuantities
{
    public class UpdateCartQuantitiesValidator : AbstractValidator<UpdateCartQuantitiesCommand>
    {
        public UpdateCartQuantitiesValidator()
        {
            RuleFor(c => c.ShoppingCartId)
                .NotEmpty()
                .WithMessage("Shopping cart id is required.");
            RuleForEach(c => c.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.ProductId)
                    .NotEmpty()
                    .WithMessage("Product id is required.");
                item.RuleFor(i => i.Quantity)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("Quantity must be a non-negative value.");
            });
        }
    }
}
