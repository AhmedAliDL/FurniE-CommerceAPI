using FluentValidation;

namespace App.Application.Coupons.Queries.GetAllCoupons
{
    public class GetAllCouponsValidator : AbstractValidator<GetAllCouponsQuery>
    {
        public GetAllCouponsValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100);
        }
    }
}
