using App.Application.Responses;
using MediatR;

namespace App.Application.Orders.Command.RequestOrderCancellation
{
    public record RequestOrderCancellationCommand(Guid OrderId, string Reason) : IRequest<ResponseResult<Guid>>;
}
