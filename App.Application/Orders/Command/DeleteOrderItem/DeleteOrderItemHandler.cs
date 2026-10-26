using App.Application.Common.Authorization;
using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Domain.Enums;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Orders.Command.DeleteOrderItem
{
    public class DeleteOrderItemHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, ILogger<DeleteOrderItemHandler> logger) : IRequestHandler<DeleteOrderItemCommand>
    {
        public async Task Handle(DeleteOrderItemCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Delete order item operation started.");

            Guid userId = currentUserService.UserId ?? throw new NotFoundException("User not found.");

            var order = await unitOfWork.Orders.FindAsync(o => o.OrderId == request.OrderId, cancellationToken)
                ?? throw new NotFoundException($"Order with id {request.OrderId} was not found.");

            if (order.UserId != userId && !currentUserService.IsInRole(SupportRoles.Admin))
                throw new ForbiddenException("You are not allowed to modify this order.");

            if (order.OrderStatus is not (OrderStatus.Pending or OrderStatus.Confirmed or OrderStatus.Processing))
                throw new ConflictException($"An order in status {order.OrderStatus} cannot be modified.");

            var payment = await unitOfWork.Payments.FindAsync(p => p.OrderId == order.OrderId, cancellationToken);
            if (payment?.PaymentStatus == "Completed")
                throw new ConflictException("A paid order cannot be modified. Use the cancellation or return workflow instead.");

            var orderItems = (await unitOfWork.OrderItems.FindAllAsync(oi => oi.OrderId == request.OrderId, cancellationToken)).ToList();
            var orderItem = orderItems.FirstOrDefault(oi => oi.ProductId == request.ProductId)
                ?? throw new NotFoundException($"Product with id {request.ProductId} was not found in order {request.OrderId}.");

            if (request.Quantity <= 0)
                throw new ValidationException([new ValidationFailure(nameof(request.Quantity), "Quantity must be greater than zero.")]);

            if (request.Quantity > orderItem.Quantity)
                throw new ValidationException([new ValidationFailure(nameof(request.Quantity), $"Only {orderItem.Quantity} unit(s) of this product remain in the order.")]);

            var product = await unitOfWork.Products.FindAsync(p => p.ProductId == request.ProductId, cancellationToken);
            if (product != null)
            {
                product.StockQuantity += request.Quantity;
                unitOfWork.Products.Update(product);
            }

            decimal removedPrice = orderItem.UnitPrice * request.Quantity;
            orderItem.Quantity -= request.Quantity;
            order.TotalPrice -= removedPrice;
            unitOfWork.Orders.Update(order);

            if (orderItem.Quantity > 0)
                unitOfWork.OrderItems.Update(orderItem);
            else
                unitOfWork.OrderItems.Delete(orderItem);

            int totalQuantity = orderItems.Where(oi => oi.OrderItemId != orderItem.OrderItemId).Sum(oi => oi.Quantity) + orderItem.Quantity;
            if (totalQuantity <= 0)
                unitOfWork.Orders.Delete(order);

            await unitOfWork.CompleteAsync(cancellationToken);
            logger.LogInformation("Delete order item operation completed.");
        }
    }
}
