using MediatR;

namespace App.Application.Wishlists.Command.AddToWishlist
{
    public record AddToWishlistCommand(Guid ProductId, decimal ProductPrice) : IRequest;
}
