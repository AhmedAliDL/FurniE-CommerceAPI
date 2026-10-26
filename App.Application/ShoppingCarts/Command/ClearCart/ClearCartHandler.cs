using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.ShoppingCarts.Command.ClearCart
{
    public class ClearCartHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, ILogger<ClearCartHandler> logger) : IRequestHandler<ClearCartCommand>
    {
        public async Task Handle(ClearCartCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Clear cart operation started.");

            Guid userId = currentUserService.UserId ?? throw new NotFoundException("User not found.");

            var userCart = await unitOfWork.ShoppingCarts.FindAsync(c => c.UserId == userId, cancellationToken);
            if (userCart == null)
            {
                logger.LogInformation("Clear cart operation completed. No cart exists for the user.");
                return;
            }

            var cartItems = await unitOfWork.CartItems.FindAllAsync(ci => ci.ShoppingCartId == userCart.Id, cancellationToken);
            if (cartItems.Count > 0)
            {
                unitOfWork.CartItems.DeleteRange(cartItems);
                await unitOfWork.CompleteAsync(cancellationToken);
            }

            logger.LogInformation("Clear cart operation completed. RemovedItems: {RemovedItems}", cartItems.Count);
        }
    }
}
