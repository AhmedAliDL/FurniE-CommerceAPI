using FluentValidation;

namespace App.Application.SupportTickets.Queries.GetAllTickets
{
    public class GetAllTicketsValidator : AbstractValidator<GetAllTicketsQuery>
    {
        public GetAllTicketsValidator()
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
