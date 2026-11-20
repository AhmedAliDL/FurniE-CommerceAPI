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

        // Optional: repeating the same key returns the original result instead of refunding again.
        public string? IdempotencyKey { get; set; }
    }
}
