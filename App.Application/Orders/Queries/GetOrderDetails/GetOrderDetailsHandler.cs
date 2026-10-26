using App.Application.Common.Authorization;
using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Orders.Dto;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Orders.Queries.GetOrderDetails
{
    public class GetOrderDetailsHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, ILogger<GetOrderDetailsHandler> logger) : IRequestHandler<GetOrderDetailsQuery, List<OrderDetailsDto>>
    {
        public async Task<List<OrderDetailsDto>> Handle(GetOrderDetailsQuery request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Get order details {OrderId} operation started.", request.OrderId);

            Guid userId = currentUserService.UserId ?? throw new NotFoundException("User not found.");

            var order = await unitOfWork.Orders.FindAsync(o => o.OrderId == request.OrderId, cancellationToken)
                ?? throw new NotFoundException($"Order with id {request.OrderId} was not found.");

            if (order.UserId != userId
                && !currentUserService.IsInRole(SupportRoles.Admin)
                && !currentUserService.IsInRole(SupportRoles.SupportAgent))
                throw new ForbiddenException("You are not allowed to view this order.");

            var orderItems = await unitOfWork.OrderItems.FindAllAsync(oi => oi.OrderId == request.OrderId, [oi => oi.Product], cancellationToken);

            var orderDetails = new List<OrderDetailsDto>();
            foreach (var orderItem in orderItems)
            {
                bool isReviewed = await unitOfWork.Reviews.FindAsync(r => r.UserId == order.UserId && r.ProductId == orderItem.ProductId, cancellationToken) != null;
                orderDetails.Add(new OrderDetailsDto
                {
                    OrderId = orderItem.OrderId,
                    OrderItemId = orderItem.OrderItemId,
                    ProductId = orderItem.ProductId,
                    ProductName = orderItem.Product?.ProductName ?? string.Empty,
                    ProductImage = orderItem.Product?.ImageUrl ?? string.Empty,
                    ProductPrice = orderItem.UnitPrice,
                    ProductQuantity = orderItem.Quantity,
                    ShippingStatus = order.OrderStatus.ToString(),
                    IsReviewed = isReviewed
                });
            }

            logger.LogInformation("Get order details {OrderId} operation completed.", request.OrderId);
            return orderDetails;
        }
    }
}
