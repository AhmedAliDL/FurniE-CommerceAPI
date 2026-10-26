using App.Application.Common.Authorization;
using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.SupportChat.Command.CloseConversation
{
    public class CloseConversationHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IIdentityService identityService, IRoleService roleService, ILogger<CloseConversationHandler> logger) : IRequestHandler<CloseConversationCommand>
    {
        public async Task Handle(CloseConversationCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Close conversation operation started.");

            Guid userId = currentUserService.UserId ?? throw new NotFoundException("User not found.");

            var ticket = await unitOfWork.SupportTickets.FindAsync(t => t.TicketId == request.TicketId, cancellationToken)
                ?? throw new NotFoundException($"Support ticket with id {request.TicketId} was not found.");

            var user = await identityService.GetUserByIdAsync(userId);
            var roles = user == null ? [] : await roleService.GetAllRolesOfUser(user);
            if (!SupportRoles.IsSupportStaff(roles))
                throw new ForbiddenException("Only support agents can close conversations.");

            var conversation = await unitOfWork.SupportConversations.FindAsync(c => c.SupportTicketId == ticket.TicketId, cancellationToken)
                ?? throw new NotFoundException("No conversation has been started for this support ticket.");

            if (conversation.Status != SupportConversationStatus.Closed)
            {
                conversation.Status = SupportConversationStatus.Closed;
                conversation.ClosedAt = DateTime.UtcNow;
                conversation.UpdatedAt = DateTime.UtcNow;
                unitOfWork.SupportConversations.Update(conversation);
                await unitOfWork.CompleteAsync(cancellationToken);
            }

            logger.LogInformation("Close conversation operation completed. TicketId: {TicketId}", ticket.TicketId);
        }
    }
}
