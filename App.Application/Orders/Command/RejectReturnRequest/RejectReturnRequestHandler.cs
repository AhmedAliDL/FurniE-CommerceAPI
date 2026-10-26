using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Responses;
using App.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Orders.Command.RejectReturnRequest
{
    public class RejectReturnRequestHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, INotificationService notificationService, ILogger<RejectReturnRequestHandler> logger) : IRequestHandler<RejectReturnRequestCommand, ResponseResult>
    {
        public async Task<ResponseResult> Handle(RejectReturnRequestCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Return request rejection started. RequestId: {RequestId}", request.RequestId);

            Guid adminId = currentUserService.UserId ?? throw new NotFoundException("User not found.");

            var returnRequest = await unitOfWork.ReturnRequests.FindAsync(r => r.Id == request.RequestId, cancellationToken)
                ?? throw new NotFoundException($"Return request with id {request.RequestId} was not found.");

            if (returnRequest.Status is not (ReturnRequestStatus.Pending or ReturnRequestStatus.Accepted))
                return ResponseResult.Failure($"Return request can no longer be rejected (current: {returnRequest.Status}).");

            returnRequest.Status = ReturnRequestStatus.Rejected;
            returnRequest.AdminNote = request.AdminNote;
            returnRequest.UpdatedAt = DateTime.UtcNow;
            unitOfWork.ReturnRequests.Update(returnRequest);

            await unitOfWork.CompleteAsync(cancellationToken);

            await notificationService.NotifyUserAsync(returnRequest.CustomerId,
                $"Your return request for order {returnRequest.OrderId} was rejected. Reason: {request.AdminNote}",
                adminId,
                $"api/orders/{returnRequest.OrderId}/details",
                cancellationToken);

            logger.LogInformation("Return request rejected. RequestId: {RequestId}", request.RequestId);
            return ResponseResult.Success();
        }
    }
}
