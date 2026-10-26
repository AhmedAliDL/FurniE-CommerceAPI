using App.Application.Common.Interfaces.Services;
using App.Application.Notifications.Dto;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace App.Infrastructure.Services
{
    public class ContactService(IConfiguration configuration, ILogger<ContactService> logger) : IContactService
    {
        private async Task<NotifyDto> SendInformationToServerAsync(MimeMessage message)
        {
            using (var client = new SmtpClient())
            {
                try
                {
                    logger.LogInformation("Send message to server operation started.");
                    await client.ConnectAsync(
                        configuration["SmtpSettings:SMTPServer"]!,
                        int.Parse(configuration["SmtpSettings:Port"]!),
                        false);

                    var userName = configuration["SmtpSettings:SmtpEmail"]!;
                    var emailPassword = configuration["SmtpSettings:Password"]!;

                    await client.AuthenticateAsync(userName, emailPassword);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                    logger.LogInformation("Send message to server operation completed.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Sending email to {Recipients} failed.", message.To);
                    return new NotifyDto
                    {
                        Success = false,
                        Message = "Error happened while sending data to server."
                    };
                }
            }
            return new NotifyDto
            {
                Success = true,
                Message = "Message sent successfuly"
            }; ;
        }
        public async Task<NotifyDto> SendEmailServiceAsync(string toEmailAdd, string fromEmailAdd, string subject, string body, string fromName, string toName, bool isHtml = false, string? replyToEmail = null, string? replyToName = null)
        {

            var message = new MimeMessage();

            message.From.Add(new MailboxAddress(fromName, fromEmailAdd));

            message.To.Add(new MailboxAddress(toName, toEmailAdd));

            if (!string.IsNullOrWhiteSpace(replyToEmail))
                message.ReplyTo.Add(new MailboxAddress(replyToName ?? fromName, replyToEmail));

            message.Subject = subject;

            message.Body = new TextPart(isHtml ? "html" : "plain")
            {
                Text = body
            };

            return await SendInformationToServerAsync(message);
        }
    }

}