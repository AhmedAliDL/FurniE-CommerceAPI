using App.Application.Responses;
using MediatR;

namespace App.Application.Orders.Command.ApproveCancellationRequest
{
    public record ApproveCancellationRequestCommand(Guid RequestId) : IRequest<ResponseResult>
    {
        // Optional: repeating the same key returns the original outcome instead of voiding again.
        public string? IdempotencyKey { get; set; }
    }
}
