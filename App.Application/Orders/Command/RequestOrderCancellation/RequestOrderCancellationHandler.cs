using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Orders.Common;
using App.Application.Responses;
using App.Domain.Entities;
using App.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Orders.Command.RequestOrderCancellation
{
    public class RequestOrderCancellationHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IIdentityService identityService, INotificationService notificationService, ILogger<RequestOrderCancellationHandler> logger) : IRequestHandler<RequestOrderCancellationCommand, ResponseResult<Guid>>
    {
        public async Task<ResponseResult<Guid>> Handle(RequestOrderCancellationCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Order cancellation request operation started. OrderId: {OrderId}", request.OrderId);

            Guid userId = currentUserService.UserId ?? throw new NotFoundException("User not found.");

            var order = await unitOfWork.Orders.FindAsync(o => o.OrderId == request.OrderId, cancellationToken)
                ?? throw new NotFoundException($"Order with id {request.OrderId} was not found.");

            if (order.UserId != userId)
                throw new ForbiddenException("You are not allowed to request cancellation for this order.");

            if (order.OrderStatus is not (OrderStatus.Confirmed or OrderStatus.Processing))
                return ResponseResult<Guid>.Failure($"An order in status {order.OrderStatus} can no longer be cancelled.");

            var payment = await unitOfWork.Payments.FindAsync(p => p.OrderId == order.OrderId, cancellationToken);
            bool isPaid = payment?.PaymentStatus == "Completed";
            if (!isPaid)
                return ResponseResult<Guid>.Failure("This order is not paid yet. You can cancel it directly from your orders list.");

            var existingPending = await unitOfWork.CancellationRequests.FindAsync(
                r => r.OrderId == order.OrderId && r.Status == CancellationRequestStatus.Pending, cancellationToken);
            if (existingPending != null)
                return ResponseResult<Guid>.Failure("A cancellation request is already pending for this order.");

            var cancellationRequest = new CancellationRequest
            {
                OrderId = order.OrderId,
                CustomerId = userId,
                Reason = request.Reason,
                PreviousOrderStatus = order.OrderStatus.ToString(),
                Status = CancellationRequestStatus.Pending
            };
            await unitOfWork.CancellationRequests.AddAsync(cancellationRequest, cancellationToken);

            order.OrderStatus = OrderStatus.CancellationRequested;
            order.UpdatedAt = DateTime.UtcNow;
            unitOfWork.Orders.Update(order);

            await unitOfWork.CompleteAsync(cancellationToken);

            var admins = await identityService.GetUsersInRoleAsync("Admin");
            foreach (var admin in admins)
            {
                await notificationService.NotifyAdminAsync(admin.Id,
                    $"New cancellation request for order {order.OrderId}. Reason: {request.Reason}",
                    userId,
                    $"api/orders/cancellation-requests/{cancellationRequest.Id}",
                    cancellationToken);
            }

            logger.LogInformation("Order cancellation request operation completed. RequestId: {RequestId}", cancellationRequest.Id);
            return ResponseResult<Guid>.Success(cancellationRequest.Id);
        }
    }
}
