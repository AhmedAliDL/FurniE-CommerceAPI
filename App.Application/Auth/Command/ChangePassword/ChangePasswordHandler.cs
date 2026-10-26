using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Auth.Command.ChangePassword
{
    public class ChangePasswordHandler(IIdentityService identityService, ICurrentUserService currentUserService, ILogger<ChangePasswordHandler> logger) : IRequestHandler<ChangePasswordCommand, bool>
    {
        public async Task<bool> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Change password operation started.");
            Guid? userId = currentUserService.UserId;
            AppUser user = await identityService.GetUserByIdAsync(userId!.Value) ?? throw new NotFoundException("User not found");
            bool res = await identityService.ChangeUserPasswordAsync(user, request.OldPassword, request.NewPassword);
            logger.LogInformation("Change password operation completed.");
            return res;

        }
    }
}
