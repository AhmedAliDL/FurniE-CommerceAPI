using App.Application.Responses;
using App.Application.SupportChat.Dto;
using MediatR;

namespace App.Application.SupportChat.Queries.GetConversationMessages
{
    public record GetConversationMessagesQuery(Guid TicketId, int Page = 1, int PageSize = 50) : IRequest<PagedResult<SupportMessageDto>>;
}
