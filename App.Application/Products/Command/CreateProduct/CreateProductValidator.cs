using FluentValidation;

namespace App.Application.Products.Command.CreateProduct
{
    public class CreateProductValidator : AbstractValidator<CreateProductCommand>
    {
        public CreateProductValidator()
        {
            RuleFor(p => p.ProductName)
                .NotEmpty()
                .MaximumLength(50)
                .WithMessage("Product name must not exceed 50 characters.");
            RuleFor(p => p.Description)
                .MinimumLength(10)
                .WithMessage("Description must be at least 10 characters long.");
            RuleFor(p => p.Price)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Price must be a non-negative value.");
            RuleFor(p => p.StockQuantity)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Stock quantity must be a non-negative value.");
            RuleFor(p => p.CategoryId)
                .NotEmpty()
                .WithMessage("Category is required.");
            RuleFor(p => p.ImageUrl)
                .NotEmpty()
                .WithMessage("Image URL is required.")
                .Must(BeAValidUrl)
                .WithMessage("Image URL must be a valid absolute URL.");
        }
        private bool BeAValidUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }
    }
}
