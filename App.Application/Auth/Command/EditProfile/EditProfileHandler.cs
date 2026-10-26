using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace App.Application.Auth.Command.EditProfile
{
    public class EditProfileHandler(IIdentityService identityService, ICurrentUserService currentUserService, ILogger<EditProfileHandler> logger) : IRequestHandler<EditProfileCommand, IdentityResult>
    {

        public async Task<IdentityResult> Handle(EditProfileCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Update profile operation started.");
            Guid? userId = currentUserService.UserId;
            var appUser = await identityService.GetUserByIdAsync(userId!.Value);
            if (appUser != null)
            {
                appUser.Email = request.Email ?? appUser.Email;
                appUser.Address = request.Address ?? appUser.Address;
                appUser.PhoneNumber = request.Phone ?? appUser.PhoneNumber;
                appUser.FName = request.FName ?? appUser.FName;
                appUser.LName = request.LName ?? appUser.LName;
                appUser.UpdatedAt = DateTime.UtcNow;
                if (request.CurrentPassword is not null && request.NewPassword is not null)
                    await identityService.ChangeUserPasswordAsync(appUser, request.CurrentPassword!, request.NewPassword!);
                var res = await identityService.UpdateUserAsync(appUser);
                logger.LogInformation("Update profile operation compeleted.");
                return res;
            }
            else
                throw new NotFoundException("User not found.");
        }
    }
}
