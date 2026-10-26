namespace App.Application.SupportTickets.Dto
{
    public sealed class AdminSupportTicketDto
    {
        public Guid TicketId { get; init; }
        public string Subject { get; init; } = string.Empty;
        public string TicketStatus { get; init; } = string.Empty;
        public string Priority { get; init; } = string.Empty;
        public string CustomerName { get; init; } = string.Empty;
        public string? ConversationStatus { get; init; }
        public DateTime? LastMessageAt { get; init; }
        public string? LastMessagePreview { get; init; }
        public int UnreadCount { get; init; }
        public DateTime CreatedAt { get; init; }
    }
}
