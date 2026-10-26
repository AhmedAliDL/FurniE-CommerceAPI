using App.Application.Payments.Dto;
using App.Application.Responses;
using MediatR;

namespace App.Application.Payments.Command.CreatePaymentSession
{
    public record CreatePaymentSessionCommand : IRequest<ResponseResult<PaymentSessionDto>>
    {
        public Guid OrderId { get; init; }

        // Optional: same key + same order replays the previously created session instead of creating a new Paymob intention.
        public string? IdempotencyKey { get; init; }
    }
}
