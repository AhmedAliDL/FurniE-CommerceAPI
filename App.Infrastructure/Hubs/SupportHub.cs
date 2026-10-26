using App.Application.Common.Exceptions;
using App.Application.SupportChat.Command.SendMessage;
using App.Application.SupportChat.Dto;
using App.Application.SupportChat.Queries.CheckTicketAccess;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace App.Infrastructure.Hubs
{
    [Authorize]
    public class SupportHub(ISender sender, ILogger<SupportHub> logger) : Hub
    {
        // SignalR on net8.0 does not bind CancellationToken parameters, so hub methods must not declare one.
        // Use Context.ConnectionAborted for the request-scoped token instead.
        public async Task JoinTicket(Guid ticketId)
        {
            logger.LogInformation("Support hub join ticket operation started.");
            if (!Guid.TryParse(Context.UserIdentifier, out Guid userId))
                throw new HubException("User not found.");

            try
            {
                await sender.Send(new CheckTicketAccessQuery(ticketId, userId), Context.ConnectionAborted);
            }
            catch (Exception ex) when (ex is ValidationException or NotFoundException or ForbiddenException)
            {
                throw new HubException(ex.Message);
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"ticket-{ticketId}");
            logger.LogInformation("Support hub join ticket operation completed. TicketId: {TicketId}", ticketId);
        }

        public async Task LeaveTicket(Guid ticketId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"ticket-{ticketId}");
        }

        public async Task<SupportMessageDto> BroadcastMessageAsync(Guid ticketId, string content)
        {
            logger.LogInformation("Support hub message sending operation started.");
            if (!Guid.TryParse(Context.UserIdentifier, out Guid userId))
                throw new HubException("User not found.");

            SupportMessageDto result;
            try
            {
                result = await sender.Send(new SendMessageCommand(ticketId, userId, content), Context.ConnectionAborted);
            }
            catch (Exception ex) when (ex is ValidationException or NotFoundException or ForbiddenException)
            {
                throw new HubException(ex.Message);
            }

            logger.LogInformation("Support hub message sending operation completed. MessageId: {MessageId}", result.MessageId);
            return result;
        }
    }
}
