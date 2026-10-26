using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Models;
using App.Application.Notifications.Dto;
using App.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace App.Application.Auth.Command.ForgetPassword
{
    public class ForgetPasswordHandler(IConfiguration configuration, IContactService contactService, IIdentityService identityService, IEmailTemplateBuilder emailTemplateBuilder, ILogger<ForgetPasswordHandler> logger) : IRequestHandler<ForgetPasswordCommand, NotifyDto>
    {
        public async Task<NotifyDto> Handle(ForgetPasswordCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Forget password operation started.");
            AppUser user = await identityService.GetUserByEmailAsync(request.Email) ?? throw new NotFoundException("User not found.");
            string token = await identityService.GenerateForgetPasswordConfirmationTokenAsync(user);

            string subject = "Reset Your Password - Furni";
            string body = emailTemplateBuilder.Build(new EmailContent
            {
                Title = "Reset Your Password",
                GreetingName = user.FName,
                Intro = "We received a request to reset the password for your Furni account. Use the confirmation code below to continue:",
                Highlight = token,
                Outro = "If you did not request a password reset, you can safely ignore this email and your password will remain unchanged."
            });

            var result = await contactService.SendEmailServiceAsync(
                 user.Email!,
                 configuration["SmtpSettings:AdminEmail"]!,
                 subject,
                 body,
                 "Furni",
                 $"{user.FName} {user.LName}".Trim(),
                 isHtml: true
             );
            logger.LogInformation("Forget password operation completed.");
            return result;
        }
    }
}
