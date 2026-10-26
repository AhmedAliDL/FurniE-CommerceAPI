using App.Application.Payments.Dto;
using App.Application.Responses;
using MediatR;

namespace App.Application.Payments.Command.RefundPayment
{
    // AmountCents omitted = refund everything still refundable.
    public record RefundPaymentCommand : IRequest<ResponseResult<PaymentOperationResultDto>>
    {
        public Guid OrderId { get; init; }

        public long? AmountCents { get; init; }
    }
}
