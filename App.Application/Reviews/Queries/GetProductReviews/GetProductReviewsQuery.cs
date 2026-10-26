using App.Application.Responses;
using App.Application.Reviews.Dto;
using MediatR;

namespace App.Application.Reviews.Queries.GetProductReviews
{
    public record GetProductReviewsQuery(Guid ProductId, int Page = 1, int PageSize = 20) : IRequest<PagedResult<ReviewDto>>;
}
