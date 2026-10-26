using App.Application.Responses;
using MediatR;

namespace App.Application.Wishlists.Command.MoveWishlistItemToCart
{
    public record MoveWishlistItemToCartCommand(Guid ProductId) : IRequest<ResponseResult<string>>;
}
