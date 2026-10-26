using MediatR;

namespace App.Application.SupportChat.Command.CloseConversation
{
    public record CloseConversationCommand(Guid TicketId) : IRequest;
}
