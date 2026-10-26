using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Wishlists.Command.AddToWishlist
{
    public class AddToWishlistHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, ILogger<AddToWishlistHandler> logger)
        : IRequestHandler<AddToWishlistCommand>
    {
        public async Task Handle(AddToWishlistCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Add product {ProductId} to wishlist operation started.", request.ProductId);
            Guid userId = currentUserService.UserId ?? throw new NotFoundException("User not found.");

            _ = await unitOfWork.Products.FindAsync(p => p.ProductId == request.ProductId, cancellationToken)
                ?? throw new NotFoundException($"Product with id {request.ProductId} was not found.");

            var existingItem = await unitOfWork.WishlistItems.FindAsync(w => w.UserId == userId && w.ProductId == request.ProductId, cancellationToken);
            if (existingItem is not null)
            {
                logger.LogInformation("Product {ProductId} is already in the wishlist of user {UserId}.", request.ProductId, userId);
                return;
            }

            await unitOfWork.WishlistItems.AddAsync(new Domain.Entities.WishlistItem
            {
                UserId = userId,
                ProductId = request.ProductId,
                ProductOriginalPrice = request.ProductPrice
            }, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);

            logger.LogInformation("Add product {ProductId} to wishlist operation completed.", request.ProductId);
        }
    }
}
