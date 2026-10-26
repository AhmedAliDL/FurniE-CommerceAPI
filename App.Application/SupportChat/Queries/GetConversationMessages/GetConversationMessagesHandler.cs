using App.Application.Common.Authorization;
using App.Application.Common.Enums;
using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Responses;
using App.Application.SupportChat.Dto;
using App.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.SupportChat.Queries.GetConversationMessages
{
    public class GetConversationMessagesHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IIdentityService identityService, IRoleService roleService, ILogger<GetConversationMessagesHandler> logger) : IRequestHandler<GetConversationMessagesQuery, PagedResult<SupportMessageDto>>
    {
        public async Task<PagedResult<SupportMessageDto>> Handle(GetConversationMessagesQuery request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Get conversation messages operation started.");

            Guid userId = currentUserService.UserId ?? throw new NotFoundException("User not found.");

            var ticket = await unitOfWork.SupportTickets.FindAsync(t => t.TicketId == request.TicketId, cancellationToken)
                ?? throw new NotFoundException($"Support ticket with id {request.TicketId} was not found.");

            if (ticket.CustomerId != userId)
            {
                var user = await identityService.GetUserByIdAsync(userId);
                var roles = user == null ? [] : await roleService.GetAllRolesOfUser(user);
                if (!SupportRoles.IsSupportStaff(roles))
                    throw new ForbiddenException("You are not allowed to view this conversation.");
            }

            var conversation = await unitOfWork.SupportConversations.FindAsync(c => c.SupportTicketId == ticket.TicketId, cancellationToken)
                ?? throw new NotFoundException("No conversation has been started for this support ticket.");

            (IReadOnlyList<SupportMessage> messages, int totalCount) = await unitOfWork.SupportMessages.FindPagedAsync(
                m => m.ConversationId == conversation.Id,
                [m => m.Sender],
                SortDirection.Ascending,
                m => m.CreatedAt,
                request.Page,
                request.PageSize,
                cancellationToken);

            var items = messages.Select(m => new SupportMessageDto
            {
                MessageId = m.Id,
                TicketId = ticket.TicketId,
                SenderId = m.SenderId,
                SenderName = $"{m.Sender?.FName} {m.Sender?.LName}".Trim(),
                Content = m.Content,
                SentAt = m.CreatedAt,
                IsRead = m.IsRead,
                ReadAt = m.ReadAt,
            }).ToList();

            logger.LogInformation("Get conversation messages operation completed. TotalCount: {TotalCount}", totalCount);
            return new PagedResult<SupportMessageDto>
            {
                Items = items,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }
    }
}
