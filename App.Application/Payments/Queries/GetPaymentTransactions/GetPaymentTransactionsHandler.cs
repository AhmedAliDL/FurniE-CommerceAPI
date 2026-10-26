using App.Application.Common.Enums;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Payments.Dto;
using App.Application.Responses;
using App.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace App.Application.Payments.Queries.GetPaymentTransactions
{
    public class GetPaymentTransactionsHandler(IUnitOfWork unitOfWork, ILogger<GetPaymentTransactionsHandler> logger) : IRequestHandler<GetPaymentTransactionsQuery, PagedResult<PaymentTransactionResponseDto>>
    {
        public async Task<PagedResult<PaymentTransactionResponseDto>> Handle(GetPaymentTransactionsQuery request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Get payment transactions operation started.");

            Expression<Func<PaymentTransaction, bool>> criteria =
                t => request.OrderId == null || t.OrderId == request.OrderId;

            (IReadOnlyList<PaymentTransaction> transactions, int totalCount) = await unitOfWork.PaymentTransactions.FindPagedAsync(
                criteria,
                [t => t.Order!.Customer],
                SortDirection.Descending,
                t => (object)t.PaymobCreatedAt,
                request.Page,
                request.PageSize,
                cancellationToken);

            var items = transactions.Select(ToDto).ToList();

            logger.LogInformation("Get payment transactions operation completed. MatchedTransactions: {TotalCount}", totalCount);
            return new PagedResult<PaymentTransactionResponseDto>
            {
                Items = items,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }

        private static PaymentTransactionResponseDto ToDto(PaymentTransaction t) => new()
        {
            Id = t.Id,
            PaymobTransactionId = t.PaymobTransactionId,
            PaymobOrderId = t.PaymobOrderId,
            OrderId = t.OrderId,
            CustomerName = t.Order?.Customer == null
                ? null
                : $"{t.Order.Customer.FName} {t.Order.Customer.LName}".Trim(),
            Amount = t.Amount,
            Currency = t.Currency,
            Status = t.Status,
            PaymentMethod = t.PaymentMethod,
            SourceType = t.SourceType,
            Is3DSecure = t.Is3DSecure,
            ErrorMessage = t.ErrorMessage,
            ResponseCode = t.ResponseCode,
            RefundedAmount = t.RefundedAmount,
            IsSettled = t.IsSettled,
            MerchantCommission = t.MerchantCommission,
            PaymobCreatedAt = t.PaymobCreatedAt,
            CreatedAt = t.CreatedAt
        };
    }
}
