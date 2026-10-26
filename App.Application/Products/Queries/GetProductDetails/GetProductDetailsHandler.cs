using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Products.Dto;
using App.Application.Products.Queries.GetAllProducts;
using App.Application.Reviews.Dto;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Products.Queries.GetProductDetails
{
    public class GetProductDetailsHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IRecentlyViewedService recentlyViewedService, ILogger<GetProductDetailsHandler> logger) : IRequestHandler<GetProductDetailsQuery, ProductDetailsDto>
    {
        public async Task<ProductDetailsDto> Handle(GetProductDetailsQuery request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Get product details operation started. {ProductId}", request.ProductId);

            var product = await unitOfWork.Products.FindAsync(p => p.ProductId == request.ProductId, [p => p.Category], cancellationToken)
                ?? throw new NotFoundException($"Product with id {request.ProductId} was not found.");

            if (currentUserService.UserId is { } viewerId)
            {
                try
                {
                    await recentlyViewedService.RecordViewAsync(viewerId, product.ProductId, cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to record recently viewed product {ProductId}.", product.ProductId);
                }
            }

            var reviews = await unitOfWork.Reviews.FindAllAsync(r => r.ProductId == request.ProductId, [r => r.Customer], cancellationToken);

            logger.LogInformation("Get product details operation completed. {ProductId}", request.ProductId);
            return new ProductDetailsDto
            {
                Product = GetAllProductsHandler.ToResponse(product),
                Reviews = reviews.Select(r => new ReviewDto
                {
                    ReviewId = r.ReviewId,
                    ProductId = r.ProductId,
                    CustomerName = $"{r.Customer?.FName} {r.Customer?.LName}".Trim(),
                    Rating = r.Rating,
                    Comment = r.Comment,
                    ReviewDate = r.CreatedAt
                }).ToList()
            };
        }
    }
}
