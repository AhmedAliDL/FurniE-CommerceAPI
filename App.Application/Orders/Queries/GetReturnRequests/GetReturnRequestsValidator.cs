using FluentValidation;

namespace App.Application.Orders.Queries.GetReturnRequests
{
    public class GetReturnRequestsValidator : AbstractValidator<GetReturnRequestsQuery>
    {
        public GetReturnRequestsValidator()
        {
            RuleFor(x => x.Status)
                .IsInEnum().When(x => x.Status.HasValue);

            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100);
        }
    }
}
