using App.Application.Responses;
using MediatR;

namespace App.Application.Orders.Command.RejectReturnRequest
{
    public record RejectReturnRequestCommand(Guid RequestId, string AdminNote) : IRequest<ResponseResult>;
}
