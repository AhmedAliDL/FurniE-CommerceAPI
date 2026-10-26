using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Common.Models;
using App.Domain.Enums;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace App.Infrastructure.BackgroundJobs
{
    public class OrderReviewReminderBackgroundJob(IContactService emailService, IEmailTemplateBuilder emailTemplateBuilder, IRecurringJobManager recurringJobManager, IIdentityService identityService, IUnitOfWork unitOfWork, IConfiguration configuration, ILogger<OrderReviewReminderBackgroundJob> logger) : IJobs
    {
        public Task ExecuteAsync()
        {
            recurringJobManager.AddOrUpdate("OrderReviewReminderBackgroundJob", () => OperationAsync(), Cron.Daily);
            return Task.CompletedTask;
        }

        public async Task OperationAsync()
        {
            logger.LogInformation("Order review reminder operation started.");

            var orders = await unitOfWork.Orders.FindAllAsync(o =>
                o.OrderStatus == OrderStatus.Delivered &&
                o.ReviewReminderSentAt == null &&
                o.CreatedAt <= DateTime.UtcNow.AddDays(-2));

            int sentCount = 0;
            foreach (var order in orders)
            {
                try
                {
                    var user = await identityService.GetUserByIdAsync(order.UserId);
                    if (user is null || string.IsNullOrWhiteSpace(user.Email))
                    {
                        logger.LogWarning("Skipping review reminder for order {OrderId}: customer {UserId} not found or has no email.", order.OrderId, order.UserId);
                        continue;
                    }

                    string subject = "How Did We Do? Leave a Review for Your Recent Purchase!";
                    string body = emailTemplateBuilder.Build(new EmailContent
                    {
                        Title = "How Did We Do?",
                        GreetingName = $"{user.FName} {user.LName}",
                        Intro = "We hope you are enjoying your recent purchase. Your feedback is invaluable to us and helps other customers make informed decisions - it takes less than a minute.",
                        Details = [new EmailDetail("Order Number", order.OrderId.ToString())],
                        ActionUrl = "https://yourstore.com/review",
                        ActionText = "Leave a Review",
                        Outro = "Thank you for your time and support!"
                    });

                    var result = await emailService.SendEmailServiceAsync(user.Email, configuration["SmtpSettings:AdminEmail"]!, subject, body, "Furni", $"{user.FName} {user.LName}", isHtml: true);
                    if (!result.Success)
                    {
                        logger.LogError("Review reminder email for order {OrderId} failed: {Message}", order.OrderId, result.Message);
                        continue;
                    }

                    order.ReviewReminderSentAt = DateTime.UtcNow;
                    order.UpdatedAt = DateTime.UtcNow;
                    unitOfWork.Orders.Update(order);
                    sentCount++;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Review reminder for order {OrderId} failed.", order.OrderId);
                }
            }

            await unitOfWork.CompleteAsync();
            logger.LogInformation("Order review reminder operation completed. {SentCount} reminders sent for {EligibleCount} eligible orders.", sentCount, orders.Count());
        }
    }
}
