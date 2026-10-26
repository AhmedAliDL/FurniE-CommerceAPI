using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Common.Models;
using App.Application.Orders.Common;
using App.Domain.Enums;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace App.Infrastructure.BackgroundJobs
{
    public class ScheduledOrderReminderBackgroundJob(IUnitOfWork unitOfWork, IIdentityService identityService, IContactService contactService, IEmailTemplateBuilder emailTemplateBuilder, IConfiguration configuration, IRecurringJobManager recurringJobManager, ILogger<ScheduledOrderReminderBackgroundJob> logger) : IJobs
    {
        private const int DaysUntilShipping = 5;
        private const int DaysUntilDelivery = 6;

        public Task ExecuteAsync()
        {
            recurringJobManager.AddOrUpdate("ScheduledOrderReminderBackgroundJob", () => OperationAsync(), Cron.Daily);
            return Task.CompletedTask;
        }

        public async Task OperationAsync()
        {
            logger.LogInformation("Scheduled order reminder operation started.");

            var dueForShipping = await unitOfWork.Orders.FindAllAsync(o =>
                o.OrderStatus == OrderStatus.Processing &&
                o.CreatedAt <= DateTime.UtcNow.AddDays(-DaysUntilShipping));

            var dueForDelivery = await unitOfWork.Orders.FindAllAsync(o =>
                (o.OrderStatus == OrderStatus.Shipped || o.OrderStatus == OrderStatus.OutForDelivery) &&
                o.CreatedAt <= DateTime.UtcNow.AddDays(-DaysUntilDelivery));

            var emails = new List<(Guid UserId, string Subject, EmailContent Content)>();
            int shippedCount = 0;
            int deliveredCount = 0;

            foreach (var order in dueForShipping)
            {
                if (!OrderStatusTransitions.CanTransition(order.OrderStatus, OrderStatus.Shipped))
                {
                    continue;
                }

                order.OrderStatus = OrderStatus.Shipped;
                order.UpdatedAt = DateTime.UtcNow;
                unitOfWork.Orders.Update(order);
                shippedCount++;
                emails.Add((order.UserId, "Your Furni order has been shipped!", new EmailContent
                {
                    Title = "Your Order Is On Its Way",
                    Intro = "Good news! Your order has been shipped and is on its way to you. You can track its progress anytime from your account.",
                    Details = [new EmailDetail("Order Number", order.OrderId.ToString())],
                    Outro = "We will let you know as soon as it arrives."
                }));
            }

            foreach (var order in dueForDelivery)
            {
                if (!OrderStatusTransitions.CanTransition(order.OrderStatus, OrderStatus.Delivered))
                {
                    continue;
                }

                order.OrderStatus = OrderStatus.Delivered;
                order.UpdatedAt = DateTime.UtcNow;
                unitOfWork.Orders.Update(order);
                deliveredCount++;

                var payment = await unitOfWork.Payments.FindAsync(p => p.OrderId == order.OrderId);
                if (payment != null)
                {
                    payment.PaymentStatus = "Completed";
                    payment.UpdatedAt = DateTime.UtcNow;
                    unitOfWork.Payments.Update(payment);
                }

                emails.Add((order.UserId, "Your Furni order has been delivered!", new EmailContent
                {
                    Title = "Your Order Has Been Delivered",
                    Intro = "Your order has been delivered. We hope you love your new purchase!",
                    Details = [new EmailDetail("Order Number", order.OrderId.ToString())],
                    Outro = "Thank you for shopping with Furni. We would love to hear what you think once you've had a chance to try it."
                }));
            }

            await unitOfWork.CompleteAsync();

            int sentCount = 0;
            foreach (var (userId, subject, content) in emails)
            {
                try
                {
                    var user = await identityService.GetUserByIdAsync(userId);
                    if (user is null || string.IsNullOrWhiteSpace(user.Email))
                    {
                        logger.LogWarning("Skipping order status email for user {UserId}: user not found or has no email.", userId);
                        continue;
                    }

                    var result = await contactService.SendEmailServiceAsync(user.Email, configuration["SmtpSettings:AdminEmail"]!, subject, emailTemplateBuilder.Build(content), "Furni", $"{user.FName} {user.LName}", isHtml: true);
                    if (!result.Success)
                    {
                        logger.LogError("Order status email to user {UserId} failed: {Message}", userId, result.Message);
                        continue;
                    }

                    sentCount++;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Order status email to user {UserId} failed.", userId);
                }
            }

            logger.LogInformation("Scheduled order reminder operation completed. {Shipped} orders shipped, {Delivered} orders delivered, {Sent} emails sent.", shippedCount, deliveredCount, sentCount);
        }
    }
}
