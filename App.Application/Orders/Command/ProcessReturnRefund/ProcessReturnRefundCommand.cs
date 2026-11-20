using App.Application.Responses;
using MediatR;

namespace App.Application.Orders.Command.ProcessReturnRefund
{
    public record ProcessReturnRefundCommand(Guid RequestId, long? AmountCents) : IRequest<ResponseResult>
    {
        // Optional: repeating the same key returns the original outcome instead of refunding again.
        public string? IdempotencyKey { get; set; }
    }
}
