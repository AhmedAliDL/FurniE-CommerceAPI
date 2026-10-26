using App.Application.Common.Interfaces.Services;
using App.Application.SupportChat.Dto;
using App.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace App.Infrastructure.Services
{
    public class SupportService(IHubContext<SupportHub> context) : ISupportService
    {
        public async Task BroadcastMessageAsync(Guid ticketId, SupportMessageDto message, CancellationToken cancellationToken = default)
        {
            await context.Clients.Group($"ticket-{ticketId}").SendAsync("newMessage", message, cancellationToken);
        }

        public async Task NotifyMessagesReadAsync(Guid ticketId, Guid readerId, CancellationToken cancellationToken = default)
        {
            await context.Clients.Group($"ticket-{ticketId}").SendAsync("messagesRead", readerId, cancellationToken);
        }
    }
}
