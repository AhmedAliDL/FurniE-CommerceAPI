using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Orders.Common;
using App.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Orders.Command.CancelOrder
{
    public class CancelOrderHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, ILogger<CancelOrderHandler> logger) : IRequestHandler<CancelOrderCommand>
    {
        public async Task Handle(CancelOrderCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Order cancellation operation started.");

            Guid userId = currentUserService.UserId ?? throw new NotFoundException("User not found.");

            var order = await unitOfWork.Orders.FindAsync(o => o.OrderId == request.OrderId, cancellationToken)
                ?? throw new NotFoundException($"Order with id {request.OrderId} was not found.");

            if (order.UserId != userId)
                throw new ForbiddenException("You are not allowed to cancel this order.");

            if (!OrderStatusTransitions.CanTransition(order.OrderStatus, OrderStatus.Cancelled))
                throw new InvalidOperationException($"An order in status {order.OrderStatus} cannot be cancelled.");

            var payment = await unitOfWork.Payments.FindAsync(p => p.OrderId == order.OrderId, cancellationToken);
            if (payment?.PaymentStatus == "Completed")
                throw new InvalidOperationException("This order is already paid. Please request a cancellation instead so the admin can release the payment hold.");

            order.OrderStatus = OrderStatus.Cancelled;
            unitOfWork.Orders.Update(order);

            var orderItems = await unitOfWork.OrderItems.FindAllAsync(oi => oi.OrderId == order.OrderId, [oi => oi.Product], cancellationToken);
            foreach (var orderItem in orderItems)
            {
                bool wasOutOfStock = orderItem.Product.StockQuantity == 0;
                orderItem.Product.StockQuantity += orderItem.Quantity;
                if (wasOutOfStock)
                    orderItem.Product.IsActive = true;

                unitOfWork.Products.Update(orderItem.Product);
            }

            if (payment != null && payment.PaymentStatus == "Pending")
            {
                payment.PaymentStatus = "Cancelled";
                unitOfWork.Payments.Update(payment);
            }

            await unitOfWork.CompleteAsync(cancellationToken);
            logger.LogInformation("Order cancellation operation completed. OrderId: {OrderId}", order.OrderId);
        }
    }
}
