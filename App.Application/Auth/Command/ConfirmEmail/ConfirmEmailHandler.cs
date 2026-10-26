using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace App.Application.Auth.Command.ConfirmEmail
{
    public class ConfirmEmailHandler(IIdentityService identityService, ILogger<ConfirmEmailHandler> logger) : IRequestHandler<ConfirmEmailCommand, IdentityResult>
    {
        public async Task<IdentityResult> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Confirm email operation started.");
            AppUser user = await identityService.GetUserByIdAsync(request.UserId) ?? throw new NotFoundException("User not found.");
            if (await identityService.CheckEmailConfirmationTokenAsync(user, request.ConfirmationToken))
            {
                user.EmailConfirmed = true;
                var res = await identityService.UpdateUserAsync(user);
                logger.LogInformation("Confirm email operation completed.");
                return res;
            }
            else
            {
                throw new ArgumentException("Token is not valid.");
            }

        }
    }

}
