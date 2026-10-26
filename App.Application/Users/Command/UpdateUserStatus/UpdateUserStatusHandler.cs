using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Users.Command.UpdateUserStatus
{
    public class UpdateUserStatusHandler(IIdentityService identityService, ICurrentUserService currentUserService, ILogger<UpdateUserStatusHandler> logger) : IRequestHandler<UpdateUserStatusCommand, bool>
    {
        public async Task<bool> Handle(UpdateUserStatusCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Update user {UserId} status operation started.", request.UserId);

            var user = await identityService.GetUserByIdAsync(request.UserId);
            if (user == null)
                return false;

            if (!request.IsActive && currentUserService.UserId == user.Id)
                throw new ConflictException("You cannot deactivate your own account.");

            user.IsActive = request.IsActive;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await identityService.UpdateUserAsync(user);
            if (!result.Succeeded)
                throw new InvalidOperationException($"Failed to update the status of user {request.UserId}.");

            logger.LogInformation("Update user {UserId} status operation completed.", request.UserId);
            return true;
        }
    }
}
