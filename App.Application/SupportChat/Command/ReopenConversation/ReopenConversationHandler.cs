using App.Application.Common.Authorization;
using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.SupportChat.Command.ReopenConversation
{
    public class ReopenConversationHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IIdentityService identityService, IRoleService roleService, ILogger<ReopenConversationHandler> logger) : IRequestHandler<ReopenConversationCommand>
    {
        public async Task Handle(ReopenConversationCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Reopen conversation operation started.");

            Guid userId = currentUserService.UserId ?? throw new NotFoundException("User not found.");

            var ticket = await unitOfWork.SupportTickets.FindAsync(t => t.TicketId == request.TicketId, cancellationToken)
                ?? throw new NotFoundException($"Support ticket with id {request.TicketId} was not found.");

            if (ticket.CustomerId != userId)
            {
                var user = await identityService.GetUserByIdAsync(userId);
                var roles = user == null ? [] : await roleService.GetAllRolesOfUser(user);
                if (!SupportRoles.IsSupportStaff(roles))
                    throw new ForbiddenException("You are not allowed to reopen this conversation.");
            }

            var conversation = await unitOfWork.SupportConversations.FindAsync(c => c.SupportTicketId == ticket.TicketId, cancellationToken)
                ?? throw new NotFoundException("No conversation has been started for this support ticket.");

            if (conversation.Status == SupportConversationStatus.Closed)
            {
                conversation.Status = SupportConversationStatus.Active;
                conversation.ClosedAt = null;
                conversation.UpdatedAt = DateTime.UtcNow;
                unitOfWork.SupportConversations.Update(conversation);
                await unitOfWork.CompleteAsync(cancellationToken);
            }

            logger.LogInformation("Reopen conversation operation completed. TicketId: {TicketId}", ticket.TicketId);
        }
    }
}
