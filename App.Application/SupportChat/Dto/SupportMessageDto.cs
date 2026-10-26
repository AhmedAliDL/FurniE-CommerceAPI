namespace App.Application.SupportChat.Dto
{
    public sealed class SupportMessageDto
    {
        public Guid MessageId { get; init; }
        public Guid TicketId { get; init; }
        public Guid SenderId { get; init; }
        public string SenderName { get; init; } = string.Empty;
        public string Content { get; init; } = string.Empty;
        public DateTime SentAt { get; init; }
        public bool IsRead { get; init; }
        public DateTime? ReadAt { get; init; }
    }
}
