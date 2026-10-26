using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Responses;
using App.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.ShoppingCarts.Command.AddCartItem
{
    public class AddCartItemHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, ILogger<AddCartItemHandler> logger) : IRequestHandler<AddCartItemCommand, ResponseResult<string>>
    {
        public async Task<ResponseResult<string>> Handle(AddCartItemCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Add cart item operation started.");

            var product = await unitOfWork.Products.FindAsync(p => p.ProductId == request.ProductId, cancellationToken)
                ?? throw new NotFoundException($"Product with id {request.ProductId} was not found.");

            if (product.StockQuantity <= 0)
                return ResponseResult<string>.Failure("Product out of stock");

            Guid userId = currentUserService.UserId ?? throw new NotFoundException("User not found.");

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

            await unitOfWork.CompleteAsync(cancellationToken);
            logger.LogInformation("Add cart item operation completed.");
            return ResponseResult<string>.Success("Item added to cart");
        }
    }
}
