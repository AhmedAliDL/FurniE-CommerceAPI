using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Models;
using App.Application.Notifications.Dto;
using App.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace App.Application.Auth.Command.SendEmailConfirmation
{
    public class SendEmailConfirmationHandler(IIdentityService identityService, IContactService contactService, IConfiguration configuration, IEmailTemplateBuilder emailTemplateBuilder, ILogger<SendEmailConfirmationHandler> logger) : IRequestHandler<SendEmailConfirmationCommand, NotifyDto>
    {
        public async Task<NotifyDto> Handle(SendEmailConfirmationCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Send email confirmation token operation started.");
            AppUser user = await identityService.GetUserByEmailAsync(request.Email) ?? throw new NotFoundException("User not found.");
            string token = await identityService.GenerateEmailConfirmationTokenAsync(user);

            string subject = "Confirm Your Email Address - Furni";
            string body = emailTemplateBuilder.Build(new EmailContent
            {
                Title = "Confirm Your Email Address",
                GreetingName = user.FName,
                Intro = "Welcome to Furni! Use the confirmation code below to verify your email address and activate your account:",
                Highlight = token,
                Outro = "If you did not create a Furni account, you can safely ignore this email."
            });

            var res = await contactService.SendEmailServiceAsync(
                 user.Email!,
                 configuration["SmtpSettings:AdminEmail"]!,
                 subject,
                 body,
                 "Furni",
                 $"{user.FName} {user.LName}".Trim(),
                 isHtml: true
             );
            logger.LogInformation("Send email confirmation token operation completed.");
            return res;
        }
    }
}
