using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.ShoppingCarts.Dto;
using App.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.ShoppingCarts.Queries.GetCartItems
{
    public class GetCartItemsHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, ILogger<GetCartItemsHandler> logger) : IRequestHandler<GetCartItemsQuery, ShoppingCartDto>
    {
        public async Task<ShoppingCartDto> Handle(GetCartItemsQuery request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Get shopping cart items operation started.");
            Guid userId = currentUserService.UserId ?? throw new NotFoundException("User not found.");

            var shoppingCart = await unitOfWork.ShoppingCarts.FindAsync(c => c.UserId == userId, cancellationToken)
                ?? new ShoppingCart
                {
                    UserId = userId
                };

            var cartItems = await unitOfWork.CartItems.FindAllAsync(ci => ci.ShoppingCartId == shoppingCart.Id, [ci => ci.Product], cancellationToken);

            logger.LogInformation("Get shopping cart items operation completed. {Count} items returned.", cartItems.Count());
            return new ShoppingCartDto
            {
                ShoppingCartId = shoppingCart.Id,
                CartItems = cartItems.Select(ToDto).ToList(),
                TotalPrice = cartItems.Sum(ci => ci.Product.Price * ci.ItemQuantity)
            };
        }

        internal static CartItemDto ToDto(CartItem cartItem) => new()
        {
            ProductId = cartItem.ProductId,
            ProductName = cartItem.Product?.ProductName ?? string.Empty,
            ImageUrl = cartItem.Product?.ImageUrl ?? string.Empty,
            Price = cartItem.Product?.Price ?? 0,
            ItemQuantity = cartItem.ItemQuantity,
            TotalPrice = (cartItem.Product?.Price ?? 0) * cartItem.ItemQuantity
        };
    }
}
