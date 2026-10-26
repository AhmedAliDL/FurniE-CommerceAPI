using App.Application.Common.Enums;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Products.Dto;
using App.Application.Products.Queries.GetAllProducts;
using App.Application.Responses;
using App.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Products.Queries.GetActiveProducts
{
    public class GetActiveProductsHandler(IUnitOfWork unitOfWork, ILogger<GetActiveProductsHandler> logger) : IRequestHandler<GetActiveProductsQuery, PagedResult<ProductResponse>>
    {
        public async Task<PagedResult<ProductResponse>> Handle(GetActiveProductsQuery request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Get active products operation started.");

            (IReadOnlyList<Product> products, int totalCount) = await unitOfWork.Products.FindPagedAsync(
                p => p.IsActive,
                [p => p.Category],
                SortDirection.Descending,
                p => p.CreatedAt,
                request.Page,
                request.PageSize,
                cancellationToken);

            logger.LogInformation("Get active products operation completed. {Count} products returned.", totalCount);
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
