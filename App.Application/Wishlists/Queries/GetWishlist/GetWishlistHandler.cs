using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Wishlists.Dto;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Wishlists.Queries.GetWishlist
{
    public class GetWishlistHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, ILogger<GetWishlistHandler> logger)
        : IRequestHandler<GetWishlistQuery, List<WishlistItemDto>>
    {
        public async Task<List<WishlistItemDto>> Handle(GetWishlistQuery request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Get wishlist operation started.");
            Guid userId = currentUserService.UserId ?? throw new NotFoundException("User not found.");

            var wishlistItems = await unitOfWork.WishlistItems.FindAllAsync(w => w.UserId == userId, [w => w.Product], cancellationToken);

            logger.LogInformation("Get wishlist operation completed. {Count} items returned.", wishlistItems.Count());
            return wishlistItems
                .OrderByDescending(w => w.CreatedAt)
                .Select(ToDto)
                .ToList();
        }

        private static WishlistItemDto ToDto(Domain.Entities.WishlistItem wishlistItem) => new()
        {
            ProductId = wishlistItem.ProductId,
            ProductName = wishlistItem.Product?.ProductName ?? string.Empty,
            Description = wishlistItem.Product?.Description ?? string.Empty,
            Price = wishlistItem.Product?.Price ?? 0,
            ImageUrl = wishlistItem.Product?.ImageUrl ?? string.Empty,
            IsAvailable = (wishlistItem.Product?.StockQuantity ?? 0) > 0,
            IsActive = wishlistItem.Product?.IsActive ?? false,
            AddedAt = wishlistItem.CreatedAt
        };
    }
}
