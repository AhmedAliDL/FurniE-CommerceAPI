using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Products.Dto;
using App.Application.Products.Queries.GetAllProducts;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Products.Queries.GetRecentlyViewed
{
    public class GetRecentlyViewedHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IRecentlyViewedService recentlyViewedService, ILogger<GetRecentlyViewedHandler> logger) : IRequestHandler<GetRecentlyViewedQuery, List<ProductResponse>>
    {
        public async Task<List<ProductResponse>> Handle(GetRecentlyViewedQuery request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Get recently viewed products operation started.");
            Guid userId = currentUserService.UserId ?? throw new NotFoundException("User not found.");

            var productIds = await recentlyViewedService.GetRecentProductIdsAsync(userId, cancellationToken);
            if (productIds.Count == 0)
            {
                logger.LogInformation("Get recently viewed products operation completed. No views recorded.");
                return [];
            }

            var products = await unitOfWork.Products.FindAllAsync(p => productIds.Contains(p.ProductId), [p => p.Category], cancellationToken);

            var result = products.OrderBy(p => productIds.IndexOf(p.ProductId)).Select(GetAllProductsHandler.ToResponse).ToList();
            logger.LogInformation("Get recently viewed products operation completed. {Count} products returned.", result.Count);
            return result;
        }
    }
}
