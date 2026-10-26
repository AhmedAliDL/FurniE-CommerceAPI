using App.Application.Common.Authorization;
using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.SupportChat.Dto;
using App.Domain.Entities;
using App.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.SupportChat.Command.SendMessage
{
    public class SendMessageHandler(IUnitOfWork unitOfWork, IIdentityService identityService, IRoleService roleService, ISupportService supportService, INotificationService notificationService, ILogger<SendMessageHandler> logger) : IRequestHandler<SendMessageCommand, SupportMessageDto>
    {
        public async Task<SupportMessageDto> Handle(SendMessageCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Support message sending operation started.");

            Guid userId = request.SenderId;

            var ticket = await unitOfWork.SupportTickets.FindAsync(t => t.TicketId == request.TicketId, cancellationToken)
                ?? throw new NotFoundException($"Support ticket with id {request.TicketId} was not found.");

            bool isAgent = ticket.CustomerId != userId && await IsSupportStaffAsync(userId);
            if (ticket.CustomerId != userId && !isAgent)
                throw new ForbiddenException("You are not allowed to send messages on this support ticket.");

            var conversation = await unitOfWork.SupportConversations.FindAsync(c => c.SupportTicketId == ticket.TicketId, cancellationToken);
            if (conversation is null)
            {
                conversation = new SupportConversation
                {
                    SupportTicketId = ticket.TicketId,
                    Status = SupportConversationStatus.Active
                };
                await unitOfWork.SupportConversations.AddAsync(conversation, cancellationToken);
            }
            else if (conversation.Status == SupportConversationStatus.Closed)
            {
                if (isAgent)
                    throw new ForbiddenException("The conversation is closed. The customer must reopen it before you can reply.");

                conversation.Status = SupportConversationStatus.Active;
                conversation.ClosedAt = null;
                conversation.UpdatedAt = DateTime.UtcNow;
                unitOfWork.SupportConversations.Update(conversation);
            }

            var message = new SupportMessage
            {
                ConversationId = conversation.Id,
                SenderId = userId,
                Content = request.Content,
                IsRead = false
            };
            await unitOfWork.SupportMessages.AddAsync(message, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);

            var sender = await identityService.GetUserByIdAsync(userId);
            var dto = new SupportMessageDto
            {
                MessageId = message.Id,
                TicketId = ticket.TicketId,
                SenderId = userId,
                SenderName = $"{sender?.FName} {sender?.LName}".Trim(),
                Content = message.Content,
                SentAt = message.CreatedAt,
                IsRead = message.IsRead,
                ReadAt = message.ReadAt
            };

            await supportService.BroadcastMessageAsync(ticket.TicketId, dto, cancellationToken);

            if (isAgent)
                await notificationService.NotifyUserAsync(ticket.CustomerId, $"New reply on your support ticket: {ticket.Subject}", userId, $"api/support-tickets/{ticket.TicketId}", cancellationToken);

            logger.LogInformation("Support message sending operation completed. MessageId: {MessageId}", message.Id);
            return dto;
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
