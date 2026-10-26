using MediatR;

namespace App.Application.Orders.Command.DeleteOrderItem
{
    public record DeleteOrderItemCommand(Guid OrderId, Guid ProductId, int Quantity) : IRequest;
}
