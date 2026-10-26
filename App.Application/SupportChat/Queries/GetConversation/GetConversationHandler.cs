using App.Application.Common.Authorization;
using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.SupportChat.Dto;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.SupportChat.Queries.GetConversation
{
    public class GetConversationHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IIdentityService identityService, IRoleService roleService, ILogger<GetConversationHandler> logger) : IRequestHandler<GetConversationQuery, SupportConversationDto>
    {
        public async Task<SupportConversationDto> Handle(GetConversationQuery request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Get conversation operation started.");

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

            logger.LogInformation("Get conversation operation completed. ConversationId: {ConversationId}", conversation.Id);
            return new SupportConversationDto
            {
                ConversationId = conversation.Id,
                TicketId = ticket.TicketId,
                Status = conversation.Status.ToString(),
                CreatedAt = conversation.CreatedAt,
                ClosedAt = conversation.ClosedAt
            };
        }
    }
}
