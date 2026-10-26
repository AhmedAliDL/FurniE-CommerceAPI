using App.Application.Responses;
using MediatR;

namespace App.Application.Orders.Command.ApproveCancellationRequest
{
    public record ApproveCancellationRequestCommand(Guid RequestId) : IRequest<ResponseResult>;
}
