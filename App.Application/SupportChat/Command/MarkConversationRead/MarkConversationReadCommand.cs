using MediatR;

namespace App.Application.SupportChat.Command.MarkConversationRead
{
    public record MarkConversationReadCommand(Guid TicketId) : IRequest<int>;
}
