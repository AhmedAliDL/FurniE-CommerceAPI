using FluentValidation;

namespace App.Application.Products.Queries.GetProductsByCategory
{
    public class GetProductsByCategoryValidator : AbstractValidator<GetProductsByCategoryQuery>
    {
        public GetProductsByCategoryValidator()
        {
            RuleFor(x => x.CategoryName)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100);
        }
    }
}
