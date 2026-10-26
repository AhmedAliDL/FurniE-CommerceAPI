using FluentValidation;

namespace App.Application.Wishlists.Command.RemoveFromWishlist
{
    public class RemoveFromWishlistValidator : AbstractValidator<RemoveFromWishlistCommand>
    {
        public RemoveFromWishlistValidator()
        {
            RuleFor(r => r.ProductId)
                .NotEmpty()
                .WithMessage("Product id is required.");
        }
    }
}
