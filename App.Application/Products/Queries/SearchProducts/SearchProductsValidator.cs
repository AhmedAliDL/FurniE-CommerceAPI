using FluentValidation;

namespace App.Application.Products.Queries.SearchProducts
{
    public class SearchProductsValidator : AbstractValidator<SearchProductsQuery>
    {
        public SearchProductsValidator()
        {
            RuleFor(x => x.Search)
                .MaximumLength(200).When(x => x.Search is not null);

            RuleFor(x => x.Category)
                .MaximumLength(100).When(x => x.Category is not null);

            RuleFor(x => x.MinPrice)
                .GreaterThanOrEqualTo(0).When(x => x.MinPrice.HasValue);

            RuleFor(x => x.MaxPrice)
                .GreaterThanOrEqualTo(0).When(x => x.MaxPrice.HasValue);

            RuleFor(x => x)
                .Must(x => x.MinPrice is null || x.MaxPrice is null || x.MinPrice <= x.MaxPrice)
                .WithMessage("MinPrice must be less than or equal to MaxPrice.");

            RuleFor(x => x.MinRating)
                .InclusiveBetween(0, 5).When(x => x.MinRating.HasValue);

            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100);
        }
    }
}
