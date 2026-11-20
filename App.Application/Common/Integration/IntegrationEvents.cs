using System.Text.Json;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Domain.Entities;

namespace App.Application.Common.Integration
{
    public static class IntegrationEvents
    {
        public const string OrderPlaced = "OrderPlaced";
        public const string OrderStatusChanged = "OrderStatusChanged";
        public const string PaymentSucceeded = "PaymentSucceeded";
        public const string PaymentFailed = "PaymentFailed";
        public const string Refunded = "Refunded";
        public const string Voided = "Voided";
        public const string ReturnRequested = "ReturnRequested";

        // Rows join the caller's transaction and are drained by IntegrationOutboxBackgroundJob.
        public static Task PublishAsync(IUnitOfWork unitOfWork, string eventType, object payload, CancellationToken cancellationToken = default) =>
            unitOfWork.IntegrationOutbox.AddAsync(new IntegrationOutboxMessage
            {
                EventType = eventType,
                Payload = JsonSerializer.Serialize(payload),
                OccurredAt = DateTime.UtcNow
            }, cancellationToken);
    }
}
