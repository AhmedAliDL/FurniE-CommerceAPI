using App.Application.Common.Enums;
using App.Application.Products.Dto;
using App.Application.Responses;
using MediatR;

namespace App.Application.Products.Queries.SearchProducts
{
    public record SearchProductsQuery(
        string? Search = null,
        string? Category = null,
        decimal? MinPrice = null,
        decimal? MaxPrice = null,
        bool InStock = false,
        double? MinRating = null,
        ProductSortBy SortBy = ProductSortBy.Newest,
        int Page = 1,
        int PageSize = 10) : IRequest<PagedResult<ProductListItemDto>>;
}
