using FluentValidation;

namespace App.Application.Products.Command.UpdateProduct
{
    public class UpdateProductValidator : AbstractValidator<UpdateProductCommand>
    {
        public UpdateProductValidator()
        {
            RuleFor(p => p.ProductId)
                .NotEmpty()
                .WithMessage("Product id is required.");
            RuleFor(p => p.ProductName)
                .MaximumLength(50)
                .When(p => p.ProductName is not null)
                .WithMessage("Product name must not exceed 50 characters.");
            RuleFor(p => p.Description)
                .MinimumLength(10)
                .When(p => p.Description is not null)
                .WithMessage("Description must be at least 10 characters long.");
            RuleFor(p => p.Price)
                .GreaterThanOrEqualTo(0)
                .When(p => p.Price is not null)
                .WithMessage("Price must be a non-negative value.");
            RuleFor(p => p.StockQuantity)
                .GreaterThanOrEqualTo(0)
                .When(p => p.StockQuantity is not null)
                .WithMessage("Stock quantity must be a non-negative value.");
        }
    }
}
