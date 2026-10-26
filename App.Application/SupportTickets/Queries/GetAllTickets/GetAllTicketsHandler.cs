using App.Application.Common.Enums;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Responses;
using App.Application.SupportTickets.Dto;
using App.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.SupportTickets.Queries.GetAllTickets
{
    public class GetAllTicketsHandler(IUnitOfWork unitOfWork, ILogger<GetAllTicketsHandler> logger) : IRequestHandler<GetAllTicketsQuery, PagedResult<AdminSupportTicketDto>>
    {
        public async Task<PagedResult<AdminSupportTicketDto>> Handle(GetAllTicketsQuery request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Get all support tickets operation started.");

            (IReadOnlyList<SupportTicket> tickets, int totalCount) = await unitOfWork.SupportTickets.FindPagedAsync(
                t => request.Status == null || t.Status == request.Status,
                [t => t.Customer],
                SortDirection.Descending,
                t => t.CreatedAt,
                request.Page,
                request.PageSize,
                cancellationToken);

            var ticketIds = tickets.Select(t => t.TicketId).ToList();
            IReadOnlyList<SupportConversation> conversations = ticketIds.Count == 0
                ? []
                : await unitOfWork.SupportConversations.FindAllAsync(c => ticketIds.Contains(c.SupportTicketId), cancellationToken);
            var conversationIds = conversations.Select(c => c.Id).ToList();
            IReadOnlyList<SupportMessage> messages = conversationIds.Count == 0
                ? []
                : await unitOfWork.SupportMessages.FindAllAsync(m => conversationIds.Contains(m.ConversationId), cancellationToken);

            var conversationByTicket = conversations.ToDictionary(c => c.SupportTicketId);
            var messagesByConversation = messages.GroupBy(m => m.ConversationId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var items = tickets.Select(t =>
            {
                conversationByTicket.TryGetValue(t.TicketId, out var conversation);
                messagesByConversation.TryGetValue(conversation?.Id ?? Guid.Empty, out var conversationMessages);
                var lastMessage = conversationMessages?.OrderByDescending(m => m.CreatedAt).FirstOrDefault();
                return new AdminSupportTicketDto
                {
                    TicketId = t.TicketId,
                    Subject = t.Subject,
                    TicketStatus = t.Status.ToString(),
                    Priority = t.Priority.ToString(),
                    CustomerName = $"{t.Customer?.FName} {t.Customer?.LName}".Trim(),
                    ConversationStatus = conversation?.Status.ToString(),
                    LastMessageAt = lastMessage?.CreatedAt,
                    LastMessagePreview = lastMessage?.Content,
                    UnreadCount = conversationMessages?.Count(m => m.SenderId == t.CustomerId && !m.IsRead) ?? 0,
                    CreatedAt = t.CreatedAt,
                };
            }).ToList();

            logger.LogInformation("Get all support tickets operation completed. TotalCount: {TotalCount}", totalCount);
            return new PagedResult<AdminSupportTicketDto>
            {
                Items = items,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }
    }
}
