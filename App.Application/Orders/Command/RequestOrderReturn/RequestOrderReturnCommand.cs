using App.Application.Orders.Dto;
using App.Application.Responses;
using MediatR;

namespace App.Application.Orders.Command.RequestOrderReturn
{
    public record RequestOrderReturnCommand(Guid OrderId, string ReasonCategory, string ReasonText, List<ReturnItemRequestDto> Items) : IRequest<ResponseResult<Guid>>;
}
