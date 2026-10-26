using App.Domain.Interceptors;

namespace App.Domain.Entities
{
    public class Notification : ISoftDeletable, IAuditLog
    {
        public Guid Id { get; set; }
        public Guid SenderId { get; set; }
        public Guid ReceiverId { get; set; }
        public string Message { get; set; } = null!;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? Link { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime DeletedAt { get; set; }

        public AppUser Sender { get; set; } = null!;
        public AppUser Receiver { get; set; } = null!;
    }
}
