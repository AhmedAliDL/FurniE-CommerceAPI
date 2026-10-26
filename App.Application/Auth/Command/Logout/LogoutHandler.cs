using App.Application.Common.Interfaces.Services;
using App.Application.Responses;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Auth.Command.Logout
{
    public class LogoutHandler(IJwtTokenService jwtTokenService, ILogger<LogoutHandler> logger) : IRequestHandler<LogoutCommand, ResponseResult<bool>>
    {
        public async Task<ResponseResult<bool>> Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Logout operation started.");
            var res = await jwtTokenService.RevokeTokenAsync(request.RefreshToken);
            logger.LogInformation("Logout operation completed.");
            return res;
        }
    }
}
