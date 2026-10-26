using App.Application.Checkout.Dto;
using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.ShoppingCarts.Dto;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Checkout.Queries.GetCheckoutDetails
{
    public class GetCheckoutDetailsHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IIdentityService identityService, ILogger<GetCheckoutDetailsHandler> logger) : IRequestHandler<GetCheckoutDetailsQuery, CheckoutDto>
    {
        public async Task<CheckoutDto> Handle(GetCheckoutDetailsQuery request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Get checkout details operation started.");
            Guid userId = currentUserService.UserId ?? throw new NotFoundException("User not found.");

            var user = await identityService.GetUserByIdAsync(userId) ?? throw new NotFoundException("User not found.");

            var shoppingCart = await unitOfWork.ShoppingCarts.FindAsync(c => c.UserId == userId, cancellationToken)
                ?? throw new NotFoundException("Shopping cart not found.");

            var cartItems = await unitOfWork.CartItems.FindAllAsync(ci => ci.ShoppingCartId == shoppingCart.Id, [ci => ci.Product], cancellationToken);

            logger.LogInformation("Get checkout details operation completed.");
            return new CheckoutDto
            {
                ShoppingCartId = shoppingCart.Id,
                CustomerName = $"{user.FName} {user.LName}".Trim(),
                Address = user.Address,
                City = user.City,
                Country = user.Country,
                CartItems = cartItems.Select(ci => new CartItemDto
                {
                    ProductId = ci.ProductId,
                    ProductName = ci.Product?.ProductName ?? string.Empty,
                    ImageUrl = ci.Product?.ImageUrl ?? string.Empty,
                    Price = ci.Product?.Price ?? 0,
                    ItemQuantity = ci.ItemQuantity,
                    TotalPrice = (ci.Product?.Price ?? 0) * ci.ItemQuantity
                }).ToList(),
                TotalPrice = cartItems.Sum(ci => ci.Product.Price * ci.ItemQuantity)
            };
        }
    }
}
