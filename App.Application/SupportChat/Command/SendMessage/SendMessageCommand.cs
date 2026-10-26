using App.Application.SupportChat.Dto;
using MediatR;

namespace App.Application.SupportChat.Command.SendMessage
{
    public record SendMessageCommand(
        Guid TicketId,
        Guid SenderId,
        string Content) : IRequest<SupportMessageDto>;
}
