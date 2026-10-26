using App.Application.SupportChat.Dto;

namespace App.Application.Common.Interfaces.Services
{
    public interface ISupportService
    {
        Task BroadcastMessageAsync(Guid ticketId, SupportMessageDto message, CancellationToken cancellationToken = default);
        Task NotifyMessagesReadAsync(Guid ticketId, Guid readerId, CancellationToken cancellationToken = default);
    }
}
