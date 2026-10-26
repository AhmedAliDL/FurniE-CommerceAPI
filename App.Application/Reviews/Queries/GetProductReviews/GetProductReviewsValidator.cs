using FluentValidation;

namespace App.Application.Reviews.Queries.GetProductReviews
{
    public class GetProductReviewsValidator : AbstractValidator<GetProductReviewsQuery>
    {
        public GetProductReviewsValidator()
        {
            RuleFor(x => x.ProductId)
                .NotEmpty();

            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100);
        }
    }
}
