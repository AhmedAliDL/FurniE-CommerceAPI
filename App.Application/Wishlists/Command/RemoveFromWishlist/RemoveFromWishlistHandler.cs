using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Wishlists.Command.RemoveFromWishlist
{
    public class RemoveFromWishlistHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, ILogger<RemoveFromWishlistHandler> logger)
        : IRequestHandler<RemoveFromWishlistCommand>
    {
        public async Task Handle(RemoveFromWishlistCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Remove product {ProductId} from wishlist operation started.", request.ProductId);
            Guid userId = currentUserService.UserId ?? throw new NotFoundException("User not found.");

            var wishlistItem = await unitOfWork.WishlistItems.FindAsync(w => w.UserId == userId && w.ProductId == request.ProductId, cancellationToken)
                ?? throw new NotFoundException($"Product with id {request.ProductId} was not found in the wishlist.");

            unitOfWork.WishlistItems.Delete(wishlistItem);
            await unitOfWork.CompleteAsync(cancellationToken);

            logger.LogInformation("Remove product {ProductId} from wishlist operation completed.", request.ProductId);
        }
    }
}
