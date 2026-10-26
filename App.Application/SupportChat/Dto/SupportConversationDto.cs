namespace App.Application.SupportChat.Dto
{
    public sealed class SupportConversationDto
    {
        public Guid ConversationId { get; init; }
        public Guid TicketId { get; init; }
        public string Status { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
        public DateTime? ClosedAt { get; init; }
    }
}
