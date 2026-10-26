using App.Application.Common.Enums;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Products.Dto;
using App.Application.Responses;
using App.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace App.Application.Products.Queries.SearchProducts
{
    public class SearchProductsHandler(IUnitOfWork unitOfWork, ILogger<SearchProductsHandler> logger) : IRequestHandler<SearchProductsQuery, PagedResult<ProductListItemDto>>
    {
        public async Task<PagedResult<ProductListItemDto>> Handle(SearchProductsQuery request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Product search operation started.");

            Expression<Func<Product, bool>> criteria = p =>
                p.IsActive &&
                (string.IsNullOrWhiteSpace(request.Search) ||
                    p.ProductName.Contains(request.Search) ||
                    p.Description.Contains(request.Search)) &&
                (request.Category == null || (p.Category != null && p.Category.CategoryName == request.Category)) &&
                (request.MinPrice == null || p.Price >= request.MinPrice) &&
                (request.MaxPrice == null || p.Price <= request.MaxPrice) &&
                (!request.InStock || p.StockQuantity > 0) &&
                (request.MinRating == null || p.Reviews.Average(r => (double?)r.Rating) >= request.MinRating);

            (Expression<Func<Product, object>> orderBy, SortDirection sortDirection) = BuildOrdering(request.SortBy);

            (IReadOnlyList<Product> products, int totalCount) = await unitOfWork.Products.FindPagedAsync(
                criteria,
                [p => p.Category, p => p.Reviews],
                sortDirection,
                orderBy,
                request.Page,
                request.PageSize,
                cancellationToken);

            var items = products.Select(ToDto).ToList();

            logger.LogInformation("Product search operation completed. MatchedProducts: {TotalCount}", totalCount);

            return new PagedResult<ProductListItemDto>
            {
                Items = items,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }

        private static (Expression<Func<Product, object>> orderBy, SortDirection sortDirection) BuildOrdering(ProductSortBy sortBy)
        {
            return sortBy switch
            {
                ProductSortBy.PriceAscending => (p => (object)p.Price, SortDirection.Ascending),
                ProductSortBy.PriceDescending => (p => (object)p.Price, SortDirection.Descending),
                ProductSortBy.Newest => (p => (object)p.CreatedAt, SortDirection.Descending),
                ProductSortBy.HighestRated => (p => (object)((double?)p.Reviews.Average(r => (double?)r.Rating) ?? 0d), SortDirection.Descending),
                ProductSortBy.Popular => (p => (object)p.OrderItems.Count, SortDirection.Descending),
                _ => (p => (object)p.CreatedAt, SortDirection.Descending)
            };
        }

        private static ProductListItemDto ToDto(Product p) => new()
        {
            ProductId = p.ProductId,
            ProductName = p.ProductName,
            Description = p.Description,
            Price = p.Price,
            StockQuantity = p.StockQuantity,
            CategoryId = p.CategoryId,
            CategoryName = p.Category?.CategoryName ?? string.Empty,
            ImageUrl = p.ImageUrl,
            IsAvailable = p.StockQuantity > 0,
            IsActive = p.IsActive,
            AverageRating = p.Reviews.Count == 0 ? 0 : Math.Round(p.Reviews.Average(r => (double)r.Rating), 1),
            ReviewCount = p.Reviews.Count
        };
    }
}
