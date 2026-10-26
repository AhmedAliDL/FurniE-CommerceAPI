using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Responses;
using App.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Orders.Command.RejectCancellationRequest
{
    public class RejectCancellationRequestHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, INotificationService notificationService, ILogger<RejectCancellationRequestHandler> logger) : IRequestHandler<RejectCancellationRequestCommand, ResponseResult>
    {
        public async Task<ResponseResult> Handle(RejectCancellationRequestCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Cancellation request rejection started. RequestId: {RequestId}", request.RequestId);

            Guid adminId = currentUserService.UserId ?? throw new NotFoundException("User not found.");

            var cancellationRequest = await unitOfWork.CancellationRequests.FindAsync(
                r => r.Id == request.RequestId,
                [r => r.Order],
                cancellationToken)
                ?? throw new NotFoundException($"Cancellation request with id {request.RequestId} was not found.");

            if (cancellationRequest.Status != CancellationRequestStatus.Pending)
                return ResponseResult.Failure($"Cancellation request is already {cancellationRequest.Status}.");

            cancellationRequest.Status = CancellationRequestStatus.Rejected;
            cancellationRequest.AdminNote = request.AdminNote;
            cancellationRequest.ProcessedAt = DateTime.UtcNow;
            cancellationRequest.UpdatedAt = DateTime.UtcNow;
            unitOfWork.CancellationRequests.Update(cancellationRequest);

            var order = cancellationRequest.Order;
            order.OrderStatus = Enum.Parse<OrderStatus>(cancellationRequest.PreviousOrderStatus);
            order.UpdatedAt = DateTime.UtcNow;
            unitOfWork.Orders.Update(order);

            await unitOfWork.CompleteAsync(cancellationToken);

            await notificationService.NotifyUserAsync(order.UserId,
                $"Your cancellation request for order {order.OrderId} was rejected. Reason: {request.AdminNote}",
                adminId,
                $"api/orders/{order.OrderId}/details",
                cancellationToken);

            logger.LogInformation("Cancellation request rejected. RequestId: {RequestId}, OrderId: {OrderId}", request.RequestId, order.OrderId);
            return ResponseResult.Success();
        }
    }
}
