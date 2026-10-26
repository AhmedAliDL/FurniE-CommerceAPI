namespace App.Application.SupportTickets.Dto
{
    public sealed class SupportTicketDto
    {
        public Guid TicketId { get; init; }
        public string Subject { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public string Priority { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
        public DateTime? ClosedAt { get; init; }
    }
}
