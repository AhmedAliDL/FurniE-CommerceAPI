using App.Application.Common.Enums;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Products.Dto;
using App.Application.Responses;
using App.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Products.Queries.GetAllProducts
{
    public class GetAllProductsHandler(IUnitOfWork unitOfWork, ILogger<GetAllProductsHandler> logger) : IRequestHandler<GetAllProductsQuery, PagedResult<ProductResponse>>
    {
        public async Task<PagedResult<ProductResponse>> Handle(GetAllProductsQuery request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Get all products operation started.");

            (IReadOnlyList<Product> products, int totalCount) = await unitOfWork.Products.FindPagedAsync(
                p => true,
                [p => p.Category],
                SortDirection.Descending,
                p => p.CreatedAt,
                request.Page,
                request.PageSize,
                cancellationToken);

            logger.LogInformation("Get all products operation completed. {Count} products returned.", totalCount);
            return new PagedResult<ProductResponse>
            {
                Items = products.Select(ToResponse).ToList(),
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }

        internal static ProductResponse ToResponse(Product product) => new()
        {
            ProductId = product.ProductId,
            ProductName = product.ProductName,
            Description = product.Description,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.CategoryName ?? string.Empty,
            ImageUrl = product.ImageUrl,
            IsAvailable = product.StockQuantity > 0,
            IsActive = product.IsActive
        };
    }
}
