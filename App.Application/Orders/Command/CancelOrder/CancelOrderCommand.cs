using MediatR;

namespace App.Application.Orders.Command.CancelOrder
{
    public record CancelOrderCommand(Guid OrderId) : IRequest;
}
