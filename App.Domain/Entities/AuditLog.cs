namespace App.Domain.Entities
{
    public class AuditLog
    {
        public Guid Id { get; set; }

        public Guid? UserId { get; set; }

        public string Action { get; set; } = null!;

        public string EntityName { get; set; } = null!;

        public Guid EntityId { get; set; }

        public string? OldValues { get; set; }

        public string? NewValues { get; set; }

        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

        public AppUser User { get; set; } = null!;
    }
}
