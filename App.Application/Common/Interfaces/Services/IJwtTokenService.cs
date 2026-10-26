using App.Application.Auth.Dto;
using App.Application.Responses;
using App.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace App.Application.Common.Interfaces.Services
{
    public interface IJwtTokenService
    {
        Task<JwtSecurityToken> CreateJWTTokenAsync(AppUser user);
        RefreshToken CreateRefreshToken();
        Task<ResponseResult<RefreshTokenDto>> RefreshTokenAsync(string token);
        Task<ResponseResult<bool>> RevokeTokenAsync(string token);
        IEnumerable<Claim> GetClaimsFromJwt(string authHeader);
    }
}
