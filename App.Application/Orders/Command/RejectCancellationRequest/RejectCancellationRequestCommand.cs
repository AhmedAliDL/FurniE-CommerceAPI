using App.Application.Responses;
using MediatR;

namespace App.Application.Orders.Command.RejectCancellationRequest
{
    public record RejectCancellationRequestCommand(Guid RequestId, string AdminNote) : IRequest<ResponseResult>;
}
