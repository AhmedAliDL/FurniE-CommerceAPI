using MediatR;

namespace App.Application.Wishlists.Command.RemoveFromWishlist
{
    public record RemoveFromWishlistCommand(Guid ProductId) : IRequest;
}
