using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Auth.Command.ResetPassword
{
    public class ResetPasswordHandler(IIdentityService identityService, ICurrentUserService currentUserService

, ILogger<ResetPasswordHandler> logger) : IRequestHandler<ResetPasswordCommand, bool>
    {
        public async Task<bool> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Reset password operation started.");
            Guid? userId = currentUserService.UserId;
            AppUser user = await identityService.GetUserByIdAsync(userId!.Value) ?? throw new NotFoundException("User not found.");
            var result = await identityService.ResetUserPasswordAsync(user, request.ConfirmationToken, request.NewPassword);
            logger.LogInformation("Reset password operation completed.");
            return result;
        }
    }
}
