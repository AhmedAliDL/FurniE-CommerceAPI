using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Responses;
using App.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Orders.Command.ReceiveReturnRequest
{
    public class ReceiveReturnRequestHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, INotificationService notificationService, ILogger<ReceiveReturnRequestHandler> logger) : IRequestHandler<ReceiveReturnRequestCommand, ResponseResult>
    {
        public async Task<ResponseResult> Handle(ReceiveReturnRequestCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Return request receive started. RequestId: {RequestId}", request.RequestId);

            Guid adminId = currentUserService.UserId ?? throw new NotFoundException("User not found.");

            var returnRequest = await unitOfWork.ReturnRequests.FindAsync(r => r.Id == request.RequestId, cancellationToken)
                ?? throw new NotFoundException($"Return request with id {request.RequestId} was not found.");

            if (returnRequest.Status != ReturnRequestStatus.Accepted)
                return ResponseResult.Failure($"Return request must be accepted before items can be marked as received (current: {returnRequest.Status}).");

            returnRequest.Status = ReturnRequestStatus.Received;
            returnRequest.ReceivedAt = DateTime.UtcNow;
            returnRequest.UpdatedAt = DateTime.UtcNow;
            unitOfWork.ReturnRequests.Update(returnRequest);

            await unitOfWork.CompleteAsync(cancellationToken);

            await notificationService.NotifyUserAsync(returnRequest.CustomerId,
                $"Your returned items for order {returnRequest.OrderId} were received and are now under inspection.",
                adminId,
                $"api/orders/{returnRequest.OrderId}/details",
                cancellationToken);

            logger.LogInformation("Return request received. RequestId: {RequestId}", request.RequestId);
            return ResponseResult.Success();
        }
    }
}
