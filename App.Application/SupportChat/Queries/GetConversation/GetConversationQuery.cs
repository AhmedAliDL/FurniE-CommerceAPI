using App.Application.SupportChat.Dto;
using MediatR;

namespace App.Application.SupportChat.Queries.GetConversation
{
    public record GetConversationQuery(Guid TicketId) : IRequest<SupportConversationDto>;
}
