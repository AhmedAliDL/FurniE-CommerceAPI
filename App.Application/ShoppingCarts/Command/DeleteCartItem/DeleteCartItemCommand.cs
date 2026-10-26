using MediatR;

namespace App.Application.ShoppingCarts.Command.DeleteCartItem
{
    public record DeleteCartItemCommand(Guid ProductId, Guid ShoppingCartId) : IRequest;
}
