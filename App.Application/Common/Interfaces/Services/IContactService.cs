using App.Application.Notifications.Dto;

namespace App.Application.Common.Interfaces.Services
{
    public interface IContactService
    {
        Task<NotifyDto> SendEmailServiceAsync(string toEmailAdd, string fromEmailAdd, string subject, string body, string fromName, string toName, bool isHtml = false, string? replyToEmail = null, string? replyToName = null);
    }
}