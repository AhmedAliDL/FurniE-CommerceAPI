using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Responses;
using App.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Orders.Command.AcceptReturnRequest
{
    public class AcceptReturnRequestHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, INotificationService notificationService, ILogger<AcceptReturnRequestHandler> logger) : IRequestHandler<AcceptReturnRequestCommand, ResponseResult>
    {
        public async Task<ResponseResult> Handle(AcceptReturnRequestCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Return request acceptance started. RequestId: {RequestId}", request.RequestId);

            Guid adminId = currentUserService.UserId ?? throw new NotFoundException("User not found.");

            var returnRequest = await unitOfWork.ReturnRequests.FindAsync(r => r.Id == request.RequestId, cancellationToken)
                ?? throw new NotFoundException($"Return request with id {request.RequestId} was not found.");

            if (returnRequest.Status != ReturnRequestStatus.Pending)
                return ResponseResult.Failure($"Return request is already {returnRequest.Status}.");

            returnRequest.Status = ReturnRequestStatus.Accepted;
            returnRequest.CourierName = request.CourierName;
            returnRequest.CourierPhone = request.CourierPhone;
            returnRequest.UpdatedAt = DateTime.UtcNow;
            unitOfWork.ReturnRequests.Update(returnRequest);

            await unitOfWork.CompleteAsync(cancellationToken);

            await notificationService.NotifyUserAsync(returnRequest.CustomerId,
                $"Your return request for order {returnRequest.OrderId} was accepted. Courier: {request.CourierName}.",
                adminId,
                $"api/orders/{returnRequest.OrderId}/details",
                cancellationToken);

            logger.LogInformation("Return request accepted. RequestId: {RequestId}", request.RequestId);
            return ResponseResult.Success();
        }
    }
}
