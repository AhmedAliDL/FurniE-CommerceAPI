using MediatR;

namespace App.Application.Orders.Command.DeleteOrder
{
    public record DeleteOrderCommand(Guid OrderId) : IRequest;
}
