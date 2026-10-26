using App.Application.Auth.Dto;
using App.Application.Common.Interfaces.Services;
using App.Application.Responses;
using App.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace App.Infrastructure.Services
{

    public class JwtTokenService(UserManager<AppUser> userManager, IConfiguration configuration) : IJwtTokenService
    {
        public async Task<JwtSecurityToken> CreateJWTTokenAsync(AppUser user)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.UserName ?? user.Email ?? string.Empty),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new("IsActive", user.IsActive.ToString()),
            };
            var roles = await userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var securityKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(configuration["Jwt:SecurityKey"]!));

            var signingCredentials = new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: configuration["Jwt:Issuer"],
                audience: configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(double.Parse(configuration["Jwt:ExpirationInMinutes"]!)),
                signingCredentials: signingCredentials);

            return token;
        }
        public RefreshToken CreateRefreshToken()
        {
            var randomNum = RandomNumberGenerator.GetBytes(32);

            return new RefreshToken
            {
                Token = Convert.ToBase64String(randomNum),
                ExpiresOn = DateTime.UtcNow.AddDays(double.Parse(configuration["Jwt:ExpirationOfRefreshTokenInDays"]!)),
            };

        }
        public async Task<ResponseResult<RefreshTokenDto>> RefreshTokenAsync(string token)
        {
            var user = await userManager.Users
                .SingleOrDefaultAsync(u => u.RefreshTokens!.Any(t => t.Token == token));
            if (user == null)
            {
                return ResponseResult<RefreshTokenDto>.Failure("User is not found.");
            }
            if (!user.IsActive)
            {
                return ResponseResult<RefreshTokenDto>.Failure("User account is deactivated.");
            }
            var refreshToken = user.RefreshTokens!.Single(t => t.Token == token);
            if (!refreshToken.IsActive)
            {
                return ResponseResult<RefreshTokenDto>.Failure("Inactive token.");
            }
            refreshToken.RevokedOn = DateTime.UtcNow;

            var newRefreshToken = CreateRefreshToken();
            user.RefreshTokens!.Add(newRefreshToken);
            await userManager.UpdateAsync(user);
            var jwtToken = await CreateJWTTokenAsync(user);

            var response = new RefreshTokenDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(jwtToken),
                Expiration = jwtToken.ValidTo,
                RefreshToken = newRefreshToken.Token,
                RefreshTokenExpiration = newRefreshToken.ExpiresOn,
            };

            return ResponseResult<RefreshTokenDto>.Success(response);

        }
        public async Task<ResponseResult<bool>> RevokeTokenAsync(string token)
        {

            var user = await userManager.Users.SingleOrDefaultAsync(u => u.RefreshTokens!.Any(t => t.Token == token));
            if (user == null)
            {
                return ResponseResult<bool>.Failure("User is not found.");
            }
            var refreshToken = user.RefreshTokens!.Single(t => t.Token == token);
            if (!refreshToken.IsActive)
            {
                return ResponseResult<bool>.Failure("Inactive token.");
            }
            refreshToken.RevokedOn = DateTime.UtcNow;

            await userManager.UpdateAsync(user);

            return ResponseResult<bool>.Success(true);
        }
        private string GetToken(string authHeader)
        {
            string token = "";
            if (authHeader != null && authHeader.StartsWith("Bearer "))
            {
                token = authHeader["Bearer ".Length..].Trim();
            }
            return token;
        }
        public IEnumerable<Claim> GetClaimsFromJwt(string authHeader)
        {
            string token = GetToken(authHeader);

            var handler = new JwtSecurityTokenHandler();

            if (handler.CanReadToken(token))
            {
                var jwtToken = handler.ReadJwtToken(token);

                var claims = jwtToken.Claims;
                return claims;
            }
            return [];
        }


    }
}
