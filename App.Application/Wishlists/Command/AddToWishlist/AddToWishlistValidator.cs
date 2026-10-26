using FluentValidation;

namespace App.Application.Wishlists.Command.AddToWishlist
{
    public class AddToWishlistValidator : AbstractValidator<AddToWishlistCommand>
    {
        public AddToWishlistValidator()
        {
            RuleFor(a => a.ProductId)
                .NotEmpty()
                .WithMessage("Product id is required.");
            RuleFor(a => a.ProductPrice)
                .NotEmpty()
                .WithMessage("Product price is required.");
        }
    }
}
