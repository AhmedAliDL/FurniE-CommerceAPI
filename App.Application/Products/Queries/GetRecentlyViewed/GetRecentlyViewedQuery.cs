using App.Application.Products.Dto;
using MediatR;

namespace App.Application.Products.Queries.GetRecentlyViewed
{
    public record GetRecentlyViewedQuery : IRequest<List<ProductResponse>>;
}
