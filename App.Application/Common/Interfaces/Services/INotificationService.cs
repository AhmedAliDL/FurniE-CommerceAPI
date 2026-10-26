namespace App.Application.Common.Interfaces.Services
{
    public interface INotificationService
    {
        Task NotifyUserAsync(Guid receiver, string message, Guid sender, string? link, CancellationToken cancellationToken = default);
        Task NotifyAdminAsync(Guid receiver, string message, Guid sender, string? link, CancellationToken cancellationToken = default);
    }
}
