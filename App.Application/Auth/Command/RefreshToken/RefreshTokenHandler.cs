using App.Application.Auth.Dto;
using App.Application.Common.Interfaces.Services;
using App.Application.Responses;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Auth.Command.RefreshToken
{
    public class RefreshTokenHandler(IJwtTokenService tokenService, ILogger<RefreshTokenHandler> logger) : IRequestHandler<RefreshTokenCommand, ResponseResult<RefreshTokenDto>>
    {
        public async Task<ResponseResult<RefreshTokenDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Refresh token operation started.");
            var res = await tokenService.RefreshTokenAsync(request.RefreshToken);
            logger.LogInformation("Refresh token operation completed.");
            return res;
        }
    }
}
