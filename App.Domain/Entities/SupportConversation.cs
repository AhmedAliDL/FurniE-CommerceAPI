using App.Domain.Enums;
using App.Domain.Interceptors;

namespace App.Domain.Entities
{
    public class SupportConversation : ISoftDeletable
    {
        public Guid Id { get; set; }
        public Guid SupportTicketId { get; set; }
        public SupportConversationStatus Status { get; set; } = SupportConversationStatus.Active;
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime DeletedAt { get; set; }

        public SupportTicket SupportTicket { get; set; } = null!;
        public ICollection<SupportMessage> Messages { get; set; } = null!;
    }
}
