using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Notifications.Command.MakeNotificationRead
{
    public class MakeNotificationReadHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, ILogger<MakeNotificationReadHandler> logger) : IRequestHandler<MakeNotificationReadCommand, int>
    {
        public async Task<int> Handle(MakeNotificationReadCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Make notification read operation started.");
            Guid userId = currentUserService.UserId ?? throw new NotFoundException("User not found.");

            Notification notification = await unitOfWork.Notifications.FindAsync(n => n.Id == request.NotificationId && n.ReceiverId == userId, cancellationToken) ?? throw new NotFoundException("Notification not found.");
            notification.IsRead = true;
            unitOfWork.Notifications.Update(notification);
            var result = await unitOfWork.CompleteAsync(cancellationToken);
            logger.LogInformation("Make notification read operation completed.");
            return result;
        }
    }
}
