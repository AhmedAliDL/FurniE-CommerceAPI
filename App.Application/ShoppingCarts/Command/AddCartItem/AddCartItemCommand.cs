using App.Application.Responses;
using MediatR;

namespace App.Application.ShoppingCarts.Command.AddCartItem
{
    public record AddCartItemCommand(Guid ProductId) : IRequest<ResponseResult<string>>;
}
