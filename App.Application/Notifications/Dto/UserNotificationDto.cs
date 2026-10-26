namespace App.Application.Notifications.Dto
{
    public record UserNotificationDto
    {
        public string Message { get; set; } = null!;
        public Guid SenderId { get; set; }
        public Guid NotificationId { get; set; }
        public string? Link { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

}
