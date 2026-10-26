using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Models;
using App.Application.Notifications.Dto;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace App.Application.Contact.Command.SendContactEmail
{
    public class SendContactEmailHandler(IContactService contactService, ICurrentUserService currentUserService, IIdentityService identityService, IEmailTemplateBuilder emailTemplateBuilder, IConfiguration configuration, ILogger<SendContactEmailHandler> logger) : IRequestHandler<SendContactEmailCommand, NotifyDto>
    {
        public async Task<NotifyDto> Handle(SendContactEmailCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Send contact email operation started.");
            Guid userId = currentUserService.UserId ?? throw new NotFoundException("User not found.");

            var user = await identityService.GetUserByIdAsync(userId) ?? throw new NotFoundException("User not found.");

            string toEmail = configuration["SmtpSettings:FurniEmail"] ?? configuration["SmtpSettings:AdminEmail"]!;
            // The SMTP account rejects unverified senders; AdminEmail is the sender every other mail flow uses.
            // The customer is exposed through Reply-To instead.
            string fromEmail = configuration["SmtpSettings:AdminEmail"]!;
            string senderName = $"{user.FName} {user.LName}".Trim();
            string body = emailTemplateBuilder.Build(new EmailContent
            {
                Title = request.Subject,
                GreetingName = "Support Team",
                Intro = $"A customer has sent a message through the contact form. Sender: {senderName} ({user.Email}).",
                Note = request.Body
            });

            var result = await contactService.SendEmailServiceAsync(
                toEmail,
                fromEmail,
                request.Subject,
                body,
                "Furni Support",
                "Furni Support",
                isHtml: true,
                replyToEmail: user.Email,
                replyToName: senderName);

            logger.LogInformation("Send contact email operation completed.");
            return result;
        }
    }
}
