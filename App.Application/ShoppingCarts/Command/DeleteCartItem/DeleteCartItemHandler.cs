using App.Application.Common.Interfaces.UnitOfWork;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.ShoppingCarts.Command.DeleteCartItem
{
    public class DeleteCartItemHandler(IUnitOfWork unitOfWork, ILogger<DeleteCartItemHandler> logger) : IRequestHandler<DeleteCartItemCommand>
    {
        public async Task Handle(DeleteCartItemCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Delete cart item operation started.");

            var cartItem = await unitOfWork.CartItems.FindAsync(ci => ci.ShoppingCartId == request.ShoppingCartId && ci.ProductId == request.ProductId, cancellationToken);
            if (cartItem != null)
            {
                unitOfWork.CartItems.Delete(cartItem);
                await unitOfWork.CompleteAsync(cancellationToken);
            }

            logger.LogInformation("Delete cart item operation completed.");
        }
    }
}
