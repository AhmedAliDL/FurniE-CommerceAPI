namespace App.Application.AuditLogs.Dto
{
    public sealed class AuditLogDto
    {
        public Guid Id { get; init; }
        public Guid? UserId { get; init; }
        public string UserEmail { get; init; } = string.Empty;
        public string Action { get; init; } = string.Empty;
        public string EntityName { get; init; } = string.Empty;
        public Guid EntityId { get; init; }
        public string? OldValues { get; init; }
        public string? NewValues { get; init; }
        public DateTime CreatedAt { get; init; }
    }
}
