using App.Application.Products.Dto;
using App.Application.Responses;
using MediatR;

namespace App.Application.Products.Queries.GetAllProducts
{
    public record GetAllProductsQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<ProductResponse>>;
}
