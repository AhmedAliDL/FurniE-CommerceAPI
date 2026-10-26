using App.Application.Common.Enums;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Products.Dto;
using App.Application.Products.Queries.GetAllProducts;
using App.Application.Responses;
using App.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Products.Queries.GetProductsByCategory
{
    public class GetProductsByCategoryHandler(IUnitOfWork unitOfWork, ILogger<GetProductsByCategoryHandler> logger) : IRequestHandler<GetProductsByCategoryQuery, PagedResult<ProductResponse>>
    {
        public async Task<PagedResult<ProductResponse>> Handle(GetProductsByCategoryQuery request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Get products by category operation started. {CategoryName}", request.CategoryName);

            (IReadOnlyList<Product> products, int totalCount) = await unitOfWork.Products.FindPagedAsync(
                p => p.IsActive && p.Category.CategoryName == request.CategoryName,
                [p => p.Category],
                SortDirection.Descending,
                p => p.CreatedAt,
                request.Page,
                request.PageSize,
                cancellationToken);

            logger.LogInformation("Get products by category operation completed. {Count} products returned.", totalCount);
            return new PagedResult<ProductResponse>
            {
                Items = products.Select(GetAllProductsHandler.ToResponse).ToList(),
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }
    }
}
