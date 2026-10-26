using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace App.Infrastructure.Services
{
    public class NotificationService(IHubContext<NotificationHub> context, IUnitOfWork unitOfWork) : INotificationService
    {
        public Task NotifyUserAsync(Guid receiver, string message, Guid sender, string? link, CancellationToken cancellationToken = default)
            => SendNotificationAsync(receiver, message, sender, link, cancellationToken);

        public Task NotifyAdminAsync(Guid receiver, string message, Guid sender, string? link, CancellationToken cancellationToken = default)
            => SendNotificationAsync(receiver, message, sender, link, cancellationToken);

        private async Task SendNotificationAsync(Guid receiver, string message, Guid sender, string? link, CancellationToken cancellationToken)
        {
            await unitOfWork.Notifications.AddAsync(new Domain.Entities.Notification
            {
                ReceiverId = receiver,
                SenderId = sender,
                Message = message,
                IsRead = false,
                Link = link
            });
            await unitOfWork.CompleteAsync(cancellationToken);

            await context.Clients.User(receiver.ToString())
                .SendAsync("newMessage", message, sender, cancellationToken);
        }
    }

}
