using App.Application.Wishlists.Dto;
using MediatR;

namespace App.Application.Wishlists.Queries.GetWishlist
{
    public record GetWishlistQuery : IRequest<List<WishlistItemDto>>;
}
