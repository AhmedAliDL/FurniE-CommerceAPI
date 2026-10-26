using MediatR;

namespace App.Application.Notifications.Command.MakeNotificationRead
{
    public record MakeNotificationReadCommand(Guid NotificationId) : IRequest<int>;
}
