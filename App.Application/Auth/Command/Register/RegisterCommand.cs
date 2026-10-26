using MediatR;
namespace App.Application.Auth.Command.Register
{
    public record RegisterCommand : IRequest<Guid>
    {
        public string FName { get; set; } = null!;
        public string LName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Phone { get; set; } = null!;
        public string Address { get; set; } = string.Empty;
        public string Password { get; set; } = null!;
        public string ConfirmPassword { get; set; } = null!;
    }
}
