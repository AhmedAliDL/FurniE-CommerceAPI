using FluentValidation;

namespace App.Application.Orders.Queries.GetUserOrders
{
    public class GetUserOrdersValidator : AbstractValidator<GetUserOrdersQuery>
    {
        public GetUserOrdersValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100);
        }
    }
}
