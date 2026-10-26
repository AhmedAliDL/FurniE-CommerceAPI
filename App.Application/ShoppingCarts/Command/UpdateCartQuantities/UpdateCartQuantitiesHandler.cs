using App.Application.Common.Interfaces.UnitOfWork;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.ShoppingCarts.Command.UpdateCartQuantities
{
    public class UpdateCartQuantitiesHandler(IUnitOfWork unitOfWork, ILogger<UpdateCartQuantitiesHandler> logger) : IRequestHandler<UpdateCartQuantitiesCommand>
    {
        public async Task Handle(UpdateCartQuantitiesCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Update cart quantities operation started.");

            foreach (var item in request.Items)
            {
                var cartItem = await unitOfWork.CartItems.FindAsync(ci => ci.ShoppingCartId == request.ShoppingCartId && ci.ProductId == item.ProductId, cancellationToken);
                if (cartItem == null) continue;

                cartItem.ItemQuantity = item.Quantity;
                unitOfWork.CartItems.Update(cartItem);
            }

            await unitOfWork.CompleteAsync(cancellationToken);
            logger.LogInformation("Update cart quantities operation completed.");
        }
    }
}
