using App.Application.ShoppingCarts.Dto;
using MediatR;

namespace App.Application.ShoppingCarts.Queries.GetCartItems
{
    public record GetCartItemsQuery : IRequest<ShoppingCartDto>;
}
