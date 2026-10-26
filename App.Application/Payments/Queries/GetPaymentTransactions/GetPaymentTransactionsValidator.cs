using FluentValidation;

namespace App.Application.Payments.Queries.GetPaymentTransactions
{
    public class GetPaymentTransactionsValidator : AbstractValidator<GetPaymentTransactionsQuery>
    {
        public GetPaymentTransactionsValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100);
        }
    }
}
