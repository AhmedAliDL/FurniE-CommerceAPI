using App.Application.Notifications.Dto;
using MediatR;

namespace App.Application.Contact.Command.SendContactEmail
{
    public record SendContactEmailCommand : IRequest<NotifyDto>
    {
        public string Subject { get; set; } = null!;
        public string Body { get; set; } = null!;
    }
}
