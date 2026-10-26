namespace App.Application.Users.Dto
{
    public sealed class UserDto
    {
        public Guid UserId { get; init; }
        public string Email { get; init; } = string.Empty;
        public string FName { get; init; } = string.Empty;
        public string LName { get; init; } = string.Empty;
        public string? PhoneNumber { get; init; }
        public bool IsActive { get; init; }
        public DateTime CreatedAt { get; init; }
        public IReadOnlyList<string> Roles { get; init; } = [];
    }
}
