namespace App.Application.Notifications.Dto
{
    public record NotifyDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
    }

}
