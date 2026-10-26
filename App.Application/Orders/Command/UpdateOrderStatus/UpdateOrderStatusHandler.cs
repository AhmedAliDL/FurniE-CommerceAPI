using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Orders.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Orders.Command.UpdateOrderStatus
{
    public class UpdateOrderStatusHandler(IUnitOfWork unitOfWork, ILogger<UpdateOrderStatusHandler> logger) : IRequestHandler<UpdateOrderStatusCommand>
    {
        public async Task Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Order status update operation started.");

            var order = await unitOfWork.Orders.FindAsync(o => o.OrderId == request.OrderId, cancellationToken)
                ?? throw new NotFoundException($"Order with id {request.OrderId} was not found.");

            if (!OrderStatusTransitions.CanTransition(order.OrderStatus, request.Status))
                throw new InvalidOperationException($"Order status cannot be changed from {order.OrderStatus} to {request.Status}.");

            order.OrderStatus = request.Status;
            // The return window is measured from UpdatedAt, so it must move with every status change.
            order.UpdatedAt = DateTime.UtcNow;
            unitOfWork.Orders.Update(order);
            await unitOfWork.CompleteAsync(cancellationToken);

            logger.LogInformation("Order status update operation completed. OrderId: {OrderId}, Status: {Status}", order.OrderId, order.OrderStatus);
        }
    }
}
