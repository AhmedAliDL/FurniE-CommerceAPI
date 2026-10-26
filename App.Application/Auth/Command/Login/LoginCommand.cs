using App.Application.Auth.Dto;
using MediatR;
using System.Text.Json.Serialization;

namespace App.Application.Auth.Command.Login
{
    public record LoginCommand : IRequest<LoginDto>
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        [JsonIgnore]
        public string? RefreshToken { get; set; } = null!;
    }
}
