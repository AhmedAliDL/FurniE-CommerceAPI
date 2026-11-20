using App.Application.Payments.Dto;
using App.Application.Responses;
using MediatR;

namespace App.Application.Payments.Command.VoidPayment
{
    public record VoidPaymentCommand : IRequest<ResponseResult<PaymentOperationResultDto>>
    {
        public Guid OrderId { get; init; }

        // Optional: repeating the same key returns the original result instead of voiding again.
        public string? IdempotencyKey { get; set; }
    }
}
