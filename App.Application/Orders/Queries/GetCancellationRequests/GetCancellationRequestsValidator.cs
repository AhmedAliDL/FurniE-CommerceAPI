using FluentValidation;

namespace App.Application.Orders.Queries.GetCancellationRequests
{
    public class GetCancellationRequestsValidator : AbstractValidator<GetCancellationRequestsQuery>
    {
        public GetCancellationRequestsValidator()
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
