using FluentValidation;

namespace App.Application.Wishlists.Command.MoveWishlistItemToCart
{
    public class MoveWishlistItemToCartValidator : AbstractValidator<MoveWishlistItemToCartCommand>
    {
        public MoveWishlistItemToCartValidator()
        {
            RuleFor(x => x.ProductId)
                .NotEmpty();
        }
    }
}
