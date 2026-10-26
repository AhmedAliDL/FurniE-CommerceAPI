using FluentValidation;

namespace App.Application.SupportTickets.Queries.GetMySupportTickets
{
    public class GetMySupportTicketsValidator : AbstractValidator<GetMySupportTicketsQuery>
    {
        public GetMySupportTicketsValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100);
        }
    }
}
