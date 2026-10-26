using App.Application.Common.Enums;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Responses;
using App.Application.Reviews.Dto;
using App.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Reviews.Queries.GetProductReviews
{
    public class GetProductReviewsHandler(IUnitOfWork unitOfWork, ILogger<GetProductReviewsHandler> logger) : IRequestHandler<GetProductReviewsQuery, PagedResult<ReviewDto>>
    {
        public async Task<PagedResult<ReviewDto>> Handle(GetProductReviewsQuery request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Get reviews for product {ProductId} operation started.", request.ProductId);

            (IReadOnlyList<Review> reviews, int totalCount) = await unitOfWork.Reviews.FindPagedAsync(
                r => r.ProductId == request.ProductId,
                [r => r.Customer],
                SortDirection.Descending,
                r => r.CreatedAt,
                request.Page,
                request.PageSize,
                cancellationToken);

            logger.LogInformation("Get reviews for product {ProductId} operation completed. {Count} reviews returned.", request.ProductId, totalCount);
            return new PagedResult<ReviewDto>
            {
                Items = reviews.Select(ToDto).ToList(),
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }

        private static ReviewDto ToDto(Review review) => new()
        {
            ReviewId = review.ReviewId,
            ProductId = review.ProductId,
            CustomerName = $"{review.Customer?.FName} {review.Customer?.LName}".Trim(),
            Rating = review.Rating,
            Comment = review.Comment,
            ReviewDate = review.CreatedAt
        };
    }
}
