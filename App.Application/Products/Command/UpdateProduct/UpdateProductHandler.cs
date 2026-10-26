using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Products.Command.UpdateProduct
{
    public class UpdateProductHandler(IUnitOfWork unitOfWork, IPriceDropDetectionBackgroundJob priceDropDetection, ILogger<UpdateProductHandler> logger) : IRequestHandler<UpdateProductCommand, bool>
    {
        public async Task<bool> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Update product {ProductId} operation started.", request.ProductId);

            var product = await unitOfWork.Products.GetByIdAsync(request.ProductId);
            if (product is null)
            {
                logger.LogWarning("Product {ProductId} was not found.", request.ProductId);
                return false;
            }

            product.ProductName = request.ProductName ?? product.ProductName;
            product.Description = request.Description ?? product.Description;
            product.StockQuantity = request.StockQuantity ?? product.StockQuantity;
            product.CategoryId = request.CategoryId ?? product.CategoryId;
            product.ImageUrl = request.ImageUrl ?? product.ImageUrl;
            product.IsActive = request.IsActive ?? product.IsActive;

            List<Guid>? customersToNotify = null;
            if (request.Price != null)
            {
                product.Price = request.Price ?? product.Price;
                customersToNotify = (await unitOfWork.WishlistItems.FindAllAsync(wl => wl.ProductId == request.ProductId && wl.ProductOriginalPrice > request.Price, cancellationToken)).Select(wl => wl.UserId).ToList();
            }

            unitOfWork.Products.Update(product);
            await unitOfWork.CompleteAsync(cancellationToken);

            if (customersToNotify is not null)
            {
                await priceDropDetection.ExecuteAsync(customersToNotify);
            }

            logger.LogInformation("Update product {ProductId} operation completed.", request.ProductId);
            return true;
        }
    }
}
