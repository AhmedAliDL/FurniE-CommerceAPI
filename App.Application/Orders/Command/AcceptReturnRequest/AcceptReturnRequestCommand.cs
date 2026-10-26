using App.Application.Responses;
using MediatR;

namespace App.Application.Orders.Command.AcceptReturnRequest
{
    public record AcceptReturnRequestCommand(Guid RequestId, string CourierName, string CourierPhone) : IRequest<ResponseResult>;
}
