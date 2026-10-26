using App.Application.Common.Interfaces.Services;
using App.Application.Common.Models;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace App.Infrastructure.BackgroundJobs
{
    public class PriceDropDetectionBackgroundJob(IContactService emailService, IEmailTemplateBuilder emailTemplateBuilder, IIdentityService identityService, IConfiguration configuration, ILogger<PriceDropDetectionBackgroundJob> logger) : IPriceDropDetectionBackgroundJob
    {
        public Task ExecuteAsync(List<Guid> customerIds)
        {
            if (customerIds.Count == 0)
            {
                return Task.CompletedTask;
            }

            BackgroundJob.Enqueue(() => OperationAsync(customerIds));
            return Task.CompletedTask;
        }

        public async Task OperationAsync(List<Guid> customerIds)
        {
            logger.LogInformation("Price drop detection operation started for {Count} customers.", customerIds.Count);

            int sentCount = 0;
            foreach (var customerId in customerIds)
            {
                try
                {
                    var user = await identityService.GetUserByIdAsync(customerId);
                    if (user is null || string.IsNullOrWhiteSpace(user.Email))
                    {
                        logger.LogWarning("Skipping price drop alert for customer {CustomerId}: user not found or has no email.", customerId);
                        continue;
                    }

                    string subject = "Price Drop Alert! - Furni";
                    string body = emailTemplateBuilder.Build(new EmailContent
                    {
                        Title = "Price Drop Alert!",
                        GreetingName = $"{user.FName} {user.LName}",
                        Intro = "Good news! The price of an item in your wishlist has dropped. Check it out now before it is gone."
                    });

                    var result = await emailService.SendEmailServiceAsync(user.Email, configuration["SmtpSettings:AdminEmail"]!, subject, body, "Furni", $"{user.FName} {user.LName}", isHtml: true);
                    if (!result.Success)
                    {
                        logger.LogError("Price drop alert for customer {CustomerId} failed: {Message}", customerId, result.Message);
                        continue;
                    }

                    sentCount++;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Price drop alert for customer {CustomerId} failed.", customerId);
                }
            }

            logger.LogInformation("Price drop detection operation completed. {SentCount} alerts sent for {Count} customers.", sentCount, customerIds.Count);
        }
    }
}
