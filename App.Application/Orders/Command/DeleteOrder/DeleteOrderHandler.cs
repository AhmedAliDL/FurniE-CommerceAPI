using App.Application.Common.Authorization;
using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Orders.Command.DeleteOrder
{
    public class DeleteOrderHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, ILogger<DeleteOrderHandler> logger) : IRequestHandler<DeleteOrderCommand>
    {
        public async Task Handle(DeleteOrderCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Delete order {OrderId} operation started.", request.OrderId);

            Guid userId = currentUserService.UserId ?? throw new NotFoundException("User not found.");

            var order = await unitOfWork.Orders.FindAsync(o => o.OrderId == request.OrderId, cancellationToken)
                ?? throw new NotFoundException($"Order with id {request.OrderId} was not found.");

            if (order.UserId != userId && !currentUserService.IsInRole(SupportRoles.Admin))
                throw new ForbiddenException("You are not allowed to delete this order.");

            if (order.OrderStatus is OrderStatus.Cancelled or OrderStatus.CancellationRequested)
                throw new ConflictException($"An order in status {order.OrderStatus} cannot be deleted because its stock has already been restored.");

            var payment = await unitOfWork.Payments.FindAsync(p => p.OrderId == order.OrderId, cancellationToken);
            if (payment?.PaymentStatus == "Completed")
                throw new ConflictException("A paid order cannot be deleted. Use the cancellation or refund workflow instead.");

            var activeCancellation = await unitOfWork.CancellationRequests.FindAsync(
                r => r.OrderId == order.OrderId && r.Status != CancellationRequestStatus.Rejected,
                cancellationToken);
            if (activeCancellation != null)
                throw new ConflictException("This order has an active cancellation request and cannot be deleted.");

            var activeReturn = await unitOfWork.ReturnRequests.FindAsync(
                r => r.OrderId == order.OrderId && r.Status != ReturnRequestStatus.Rejected,
                cancellationToken);
            if (activeReturn != null)
                throw new ConflictException("This order has an active return request and cannot be deleted.");

            var orderItems = await unitOfWork.OrderItems.FindAllAsync(oi => oi.OrderId == request.OrderId, cancellationToken);
            foreach (var orderItem in orderItems)
            {
                var product = await unitOfWork.Products.FindAsync(p => p.ProductId == orderItem.ProductId, cancellationToken);
                if (product != null)
                {
                    product.StockQuantity += orderItem.Quantity;
                    unitOfWork.Products.Update(product);
                }
            }

            unitOfWork.OrderItems.DeleteRange(orderItems);
            unitOfWork.Orders.Delete(order);
            await unitOfWork.CompleteAsync(cancellationToken);
            logger.LogInformation("Delete order {OrderId} operation completed.", request.OrderId);
        }
    }
}
