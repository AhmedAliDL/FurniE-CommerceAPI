using FluentValidation;

namespace App.Application.Products.Queries.GetActiveProducts
{
    public class GetActiveProductsValidator : AbstractValidator<GetActiveProductsQuery>
    {
        public GetActiveProductsValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100);
        }
    }
}
