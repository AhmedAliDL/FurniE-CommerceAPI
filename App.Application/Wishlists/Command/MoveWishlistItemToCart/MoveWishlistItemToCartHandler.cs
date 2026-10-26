using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Responses;
using App.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Wishlists.Command.MoveWishlistItemToCart
{
    public class MoveWishlistItemToCartHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, ILogger<MoveWishlistItemToCartHandler> logger) : IRequestHandler<MoveWishlistItemToCartCommand, ResponseResult<string>>
    {
        public async Task<ResponseResult<string>> Handle(MoveWishlistItemToCartCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Move product {ProductId} from wishlist to cart operation started.", request.ProductId);

            Guid userId = currentUserService.UserId ?? throw new NotFoundException("User not found.");

            var wishlistItem = await unitOfWork.WishlistItems.FindAsync(w => w.UserId == userId && w.ProductId == request.ProductId, cancellationToken)
                ?? throw new NotFoundException($"Product with id {request.ProductId} was not found in the wishlist.");

            var product = await unitOfWork.Products.FindAsync(p => p.ProductId == request.ProductId, cancellationToken)
                ?? throw new NotFoundException($"Product with id {request.ProductId} was not found.");

            if (product.StockQuantity <= 0)
                return ResponseResult<string>.Failure("Product out of stock");

            var userCart = await unitOfWork.ShoppingCarts.FindAsync(c => c.UserId == userId, cancellationToken);
            if (userCart == null)
            {
                userCart = new ShoppingCart
                {
                    UserId = userId
                };
                await unitOfWork.ShoppingCarts.AddAsync(userCart, cancellationToken);
            }

            var cartItem = await unitOfWork.CartItems.FindAsync(ci => ci.ShoppingCartId == userCart.Id && ci.ProductId == request.ProductId, cancellationToken);
            if (cartItem == null)
            {
                cartItem = new CartItem
                {
                    ShoppingCartId = userCart.Id,
                    ProductId = request.ProductId,
                    ItemQuantity = 1
                };
                await unitOfWork.CartItems.AddAsync(cartItem, cancellationToken);
            }
            else
            {
                cartItem.ItemQuantity += 1;
                unitOfWork.CartItems.Update(cartItem);
            }

            unitOfWork.WishlistItems.Delete(wishlistItem);
            await unitOfWork.CompleteAsync(cancellationToken);

            logger.LogInformation("Move product {ProductId} from wishlist to cart operation completed.", request.ProductId);
            return ResponseResult<string>.Success("Item moved to cart");
        }
    }
}
