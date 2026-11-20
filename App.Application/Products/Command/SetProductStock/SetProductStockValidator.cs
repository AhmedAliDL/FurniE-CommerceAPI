using FluentValidation;

namespace App.Application.Products.Command.SetProductStock
{
    public class SetProductStockValidator : AbstractValidator<SetProductStockCommand>
    {
        public SetProductStockValidator()
        {
            RuleFor(p => p.ProductId)
                .NotEmpty()
                .WithMessage("Product id is required.");
            RuleFor(p => p.Quantity)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Stock quantity must be a non-negative value.");
        }
    }
}
