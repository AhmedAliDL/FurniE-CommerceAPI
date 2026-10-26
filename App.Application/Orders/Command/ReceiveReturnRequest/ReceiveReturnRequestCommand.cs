using App.Application.Responses;
using MediatR;

namespace App.Application.Orders.Command.ReceiveReturnRequest
{
    public record ReceiveReturnRequestCommand(Guid RequestId) : IRequest<ResponseResult>;
}
