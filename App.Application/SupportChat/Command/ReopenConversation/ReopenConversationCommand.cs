using MediatR;

namespace App.Application.SupportChat.Command.ReopenConversation
{
    public record ReopenConversationCommand(Guid TicketId) : IRequest;
}
