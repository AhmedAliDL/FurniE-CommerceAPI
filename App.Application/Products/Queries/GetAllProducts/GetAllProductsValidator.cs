using FluentValidation;

namespace App.Application.Products.Queries.GetAllProducts
{
    public class GetAllProductsValidator : AbstractValidator<GetAllProductsQuery>
    {
        public GetAllProductsValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100);
        }
    }
}
