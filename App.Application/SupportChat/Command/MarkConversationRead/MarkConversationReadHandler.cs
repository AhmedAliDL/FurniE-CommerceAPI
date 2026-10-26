using App.Application.Common.Authorization;
using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.SupportChat.Command.MarkConversationRead
{
    public class MarkConversationReadHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IIdentityService identityService, IRoleService roleService, ISupportService supportService, ILogger<MarkConversationReadHandler> logger) : IRequestHandler<MarkConversationReadCommand, int>
    {
        public async Task<int> Handle(MarkConversationReadCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Mark conversation read operation started.");

            Guid userId = currentUserService.UserId ?? throw new NotFoundException("User not found.");

            var ticket = await unitOfWork.SupportTickets.FindAsync(t => t.TicketId == request.TicketId, cancellationToken)
                ?? throw new NotFoundException($"Support ticket with id {request.TicketId} was not found.");

            if (ticket.CustomerId != userId && !await IsSupportStaffAsync(userId))
                throw new ForbiddenException("You are not allowed to access this support ticket.");

            var conversation = await unitOfWork.SupportConversations.FindAsync(c => c.SupportTicketId == ticket.TicketId, cancellationToken)
                ?? throw new NotFoundException("No conversation has been started for this support ticket.");

            var unread = await unitOfWork.SupportMessages.FindAllAsync(m => m.ConversationId == conversation.Id && m.SenderId != userId && !m.IsRead, cancellationToken);
            foreach (var message in unread)
            {
                message.IsRead = true;
                message.ReadAt = DateTime.UtcNow;
            }
            unitOfWork.SupportMessages.UpdateRange(unread);

            var markedCount = await unitOfWork.CompleteAsync(cancellationToken);

            await supportService.NotifyMessagesReadAsync(ticket.TicketId, userId, cancellationToken);

            logger.LogInformation("Mark conversation read operation completed. Marked: {MarkedCount}", markedCount);
            return markedCount;
        }

        private async Task<bool> IsSupportStaffAsync(Guid userId)
        {
            var user = await identityService.GetUserByIdAsync(userId);
            if (user is null)
                return false;
            var roles = await roleService.GetAllRolesOfUser(user);
            return SupportRoles.IsSupportStaff(roles);
        }
    }
}
