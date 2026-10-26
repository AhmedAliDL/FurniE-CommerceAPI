using App.Application.Auth.Dto;
using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;

namespace App.Application.Auth.Command.Login
{
    public class LoginHandler(IIdentityService identityService, IJwtTokenService tokenService, ILogger<LoginHandler> logger) : IRequestHandler<LoginCommand, LoginDto>
    {
        public async Task<LoginDto> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Login operation started.");

            AppUser? user = await identityService.GetUserByEmailAsync(request.Email) ?? throw new ForbiddenException("Password or Email is invalid.");
            if (!user.IsActive)
                throw new ForbiddenException("This account has been deactivated.");
            if (!user.EmailConfirmed)
                throw new ForbiddenException("Email not confirmed");
            var checkPassword = await identityService.CheckCorrectnessOfPasswordAsync(user, request.Password);
            if (!checkPassword)
                throw new ForbiddenException("Password or Email is invalid.");
            var token = await tokenService.CreateJWTTokenAsync(user);
            var response = new LoginDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Expiration = token.ValidTo
            };
            if (user.RefreshTokens!.Any(rt => rt.IsActive))
            {
                var activeRefreshToken = user.RefreshTokens.SingleOrDefault(t => t.IsActive);
                response.RefreshToken = activeRefreshToken!.Token;
                response.RefreshTokenExpiration = activeRefreshToken.ExpiresOn;
            }
            else
            {
                var refreshToken = tokenService.CreateRefreshToken();
                response.RefreshToken = refreshToken.Token;
                response.RefreshTokenExpiration = refreshToken.ExpiresOn;
                user.RefreshTokens.Add(refreshToken);
            }
            user.LastLoginAt = DateTime.UtcNow;
            await identityService.UpdateUserAsync(user);
            logger.LogInformation("Login operation completed.");
            return response;
        }
    }
}
