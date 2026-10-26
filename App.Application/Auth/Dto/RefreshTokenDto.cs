using System.Text.Json.Serialization;

namespace App.Application.Auth.Dto
{
    public record RefreshTokenDto
    {
        public string Token { get; set; } = null!;
        public DateTime Expiration { get; set; }
        [JsonIgnore]
        public string? RefreshToken { get; set; }
        public DateTime RefreshTokenExpiration { get; set; }
    }
}
