using System.Text;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace App.Infrastructure.BackgroundJobs
{
    public class IntegrationOutboxBackgroundJob(IUnitOfWork unitOfWork, IHttpClientFactory httpClientFactory, IConfiguration configuration, IRecurringJobManager recurringJobManager, ILogger<IntegrationOutboxBackgroundJob> logger) : IJobs
    {
        private const int MaxAttempts = 10;
        private const int BatchSize = 50;

        public Task ExecuteAsync()
        {
            recurringJobManager.AddOrUpdate("IntegrationOutboxBackgroundJob", () => OperationAsync(), Cron.Minutely);
            return Task.CompletedTask;
        }

        public async Task OperationAsync()
        {
            var endpoint = configuration["Integration:EventsEndpoint"];
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                logger.LogDebug("ERP outbox dispatch skipped: Integration:EventsEndpoint is not configured.");
                return;
            }

            var pending = (await unitOfWork.IntegrationOutbox.FindAllAsync(m => m.ProcessedAt == null && m.Attempts < MaxAttempts))
                .OrderBy(m => m.OccurredAt)
                .Take(BatchSize)
                .ToList();
            if (pending.Count == 0)
                return;

            var client = httpClientFactory.CreateClient();
            var apiKey = configuration["Integration:ApiKeys"]?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
            int sent = 0;

            foreach (var message in pending)
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new StringContent(message.Payload, Encoding.UTF8, "application/json")
                };
                request.Headers.Add("X-Event-Id", message.Id.ToString());
                request.Headers.Add("X-Event-Type", message.EventType);
                if (!string.IsNullOrWhiteSpace(apiKey))
                    request.Headers.Add("X-Api-Key", apiKey);

                try
                {
                    using var response = await client.SendAsync(request);
                    if (!response.IsSuccessStatusCode)
                        throw new HttpRequestException($"ERP endpoint returned {(int)response.StatusCode}.");

                    message.ProcessedAt = DateTime.UtcNow;
                    message.LastError = null;
                    sent++;
                }
                catch (Exception ex)
                {
                    message.Attempts++;
                    message.LastError = ex.Message.Length > 1900 ? ex.Message[..1900] : ex.Message;
                    logger.LogWarning(ex, "ERP outbox delivery failed for event {EventId}. Attempt {Attempts}.", message.Id, message.Attempts);
                }

                unitOfWork.IntegrationOutbox.Update(message);
            }

            await unitOfWork.CompleteAsync();
            logger.LogInformation("ERP outbox dispatch completed. Sent: {Sent}, Failed: {Failed}", sent, pending.Count - sent);
        }
    }
}
