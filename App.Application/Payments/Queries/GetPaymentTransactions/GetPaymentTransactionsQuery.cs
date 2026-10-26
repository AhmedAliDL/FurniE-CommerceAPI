using App.Application.Payments.Dto;
using App.Application.Responses;
using MediatR;

namespace App.Application.Payments.Queries.GetPaymentTransactions
{
    public record GetPaymentTransactionsQuery : IRequest<PagedResult<PaymentTransactionResponseDto>>
    {
        public Guid? OrderId { get; init; }
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;
    }
}
