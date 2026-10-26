using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Responses;
using App.Domain.Entities;
using App.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Orders.Command.RequestOrderReturn
{
    public class RequestOrderReturnHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IIdentityService identityService, INotificationService notificationService, ILogger<RequestOrderReturnHandler> logger) : IRequestHandler<RequestOrderReturnCommand, ResponseResult<Guid>>
    {
        private static readonly TimeSpan ReturnWindow = TimeSpan.FromDays(14);

        public async Task<ResponseResult<Guid>> Handle(RequestOrderReturnCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Order return request operation started. OrderId: {OrderId}", request.OrderId);

            Guid userId = currentUserService.UserId ?? throw new NotFoundException("User not found.");

            var order = await unitOfWork.Orders.FindAsync(o => o.OrderId == request.OrderId, cancellationToken)
                ?? throw new NotFoundException($"Order with id {request.OrderId} was not found.");

            if (order.UserId != userId)
                throw new ForbiddenException("You are not allowed to request a return for this order.");

            if (order.OrderStatus != OrderStatus.Delivered)
                return ResponseResult<Guid>.Failure("Only delivered orders can be returned.");

            // Delivery time is tracked as UpdatedAt at the moment the order reached Delivered;
            // the 14-day return window is measured from it.
            if (order.UpdatedAt == null || DateTime.UtcNow - order.UpdatedAt.Value > ReturnWindow)
                return ResponseResult<Guid>.Failure("Returns are only allowed within 14 days after delivery.");

            var activeRequest = await unitOfWork.ReturnRequests.FindAsync(
                r => r.OrderId == order.OrderId && r.Status != ReturnRequestStatus.Rejected && r.Status != ReturnRequestStatus.Completed,
                cancellationToken);
            if (activeRequest != null)
                return ResponseResult<Guid>.Failure("A return request is already active for this order.");

            var orderItems = (await unitOfWork.OrderItems.FindAllAsync(oi => oi.OrderId == order.OrderId, [oi => oi.Product], cancellationToken))
                .ToDictionary(oi => oi.OrderItemId);

            var returnRequest = new ReturnRequest
            {
                OrderId = order.OrderId,
                CustomerId = userId,
                ReasonCategory = string.Equals(request.ReasonCategory, ReturnReasonCategory.MerchantDefect.ToString(), StringComparison.OrdinalIgnoreCase)
                    ? ReturnReasonCategory.MerchantDefect
                    : ReturnReasonCategory.CustomerChoice,
                ReasonText = request.ReasonText,
                Status = ReturnRequestStatus.Pending
            };

            var returnItems = new List<ReturnItem>();
            foreach (var item in request.Items)
            {
                if (!orderItems.TryGetValue(item.OrderItemId, out var orderItem))
                    return ResponseResult<Guid>.Failure($"Order item {item.OrderItemId} does not belong to this order.");

                if (item.Quantity > orderItem.Quantity)
                    return ResponseResult<Guid>.Failure($"Cannot return {item.Quantity} units of {orderItem.Product.ProductName}; only {orderItem.Quantity} were ordered.");

                returnItems.Add(new ReturnItem
                {
                    OrderItemId = orderItem.OrderItemId,
                    ProductName = orderItem.Product.ProductName,
                    Quantity = item.Quantity
                });
            }
            returnRequest.ReturnItems = returnItems;

            await unitOfWork.ReturnRequests.AddAsync(returnRequest, cancellationToken);

            await unitOfWork.CompleteAsync(cancellationToken);

            var admins = await identityService.GetUsersInRoleAsync("Admin");
            foreach (var admin in admins)
            {
                await notificationService.NotifyAdminAsync(admin.Id,
                    $"New return request for order {order.OrderId}.",
                    userId,
                    $"api/orders/return-requests/{returnRequest.Id}",
                    cancellationToken);
            }

            logger.LogInformation("Order return request operation completed. RequestId: {RequestId}", returnRequest.Id);
            return ResponseResult<Guid>.Success(returnRequest.Id);
        }
    }
}
