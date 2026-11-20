namespace App.Domain.Entities
{
    public class IdempotencyRecord
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        // Legacy rows predate this column and carry an empty value; readers must treat '' as a wildcard.
        public string Operation { get; set; } = string.Empty;
        public string IdempotencyKey { get; set; } = string.Empty;
        public Guid? OrderId { get; set; }
        public string? ResponsePayload { get; set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public AppUser User { get; set; } = null!;
        public Order? Order { get; set; }

    }
}
