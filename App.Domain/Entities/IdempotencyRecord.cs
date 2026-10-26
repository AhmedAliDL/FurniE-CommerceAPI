namespace App.Domain.Entities
{
    public class IdempotencyRecord
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string IdempotencyKey { get; set; } = string.Empty;
        public Guid? OrderId { get; set; }
        public string? ResponsePayload { get; set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public AppUser User { get; set; } = null!;
        public Order? Order { get; set; }

    }
}
