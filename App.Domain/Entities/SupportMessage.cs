using App.Domain.Interceptors;

namespace App.Domain.Entities
{
    public class SupportMessage : ISoftDeletable
    {
        public Guid Id { get; set; }
        public Guid ConversationId { get; set; }
        public Guid SenderId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime DeletedAt { get; set; }

        public SupportConversation Conversation { get; set; } = null!;
        public AppUser Sender { get; set; } = null!;
    }
}
