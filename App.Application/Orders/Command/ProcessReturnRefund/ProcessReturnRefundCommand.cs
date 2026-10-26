using App.Application.Responses;
using MediatR;

namespace App.Application.Orders.Command.ProcessReturnRefund
{
    public record ProcessReturnRefundCommand(Guid RequestId, long? AmountCents) : IRequest<ResponseResult>;
}
