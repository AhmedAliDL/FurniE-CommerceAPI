using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace App.Infrastructure.BackgroundJobs
{
    public class CartExpirationBackgroundJob(IUnitOfWork unitOfWork, IRecurringJobManager recurringJobManager, ILogger<CartExpirationBackgroundJob> logger) : IJobs
    {
        public Task ExecuteAsync()
        {
            recurringJobManager.AddOrUpdate("CartExpirationBackgroundJob", () => OperationAsync(), Cron.Weekly);

            return Task.CompletedTask;
        }

        public async Task OperationAsync()
        {
            logger.LogInformation("Cart expiration operation started.");

            var cartItems = await unitOfWork.CartItems.FindAllAsync(ci => ci.CreatedAt <= DateTime.UtcNow.AddDays(-7));

            foreach (var cartItem in cartItems)
            {
                unitOfWork.CartItems.Delete(cartItem);
            }

            await unitOfWork.CompleteAsync();
            logger.LogInformation("Cart expiration operation completed. {Count} expired cart items removed.", cartItems.Count());
        }
    }
}
