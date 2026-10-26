using App.Application.Auth.Dto;
using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Auth.Queries.GetProfile
{
    public class GetProfileHandler(IIdentityService identityService, ICurrentUserService currentUserService, ILogger<GetProfileHandler> logger) : IRequestHandler<GetProfileQuery, ProfileDto?>
    {
        public async Task<ProfileDto?> Handle(GetProfileQuery request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Get profile operation started.");
            Guid? userId = currentUserService.UserId;
            var user = await identityService.GetUserByIdAsync(userId!.Value) ?? throw new NotFoundException("User not found.");
            var res = new ProfileDto
            {
                FName = user.FName,
                LName = user.LName,
                Address = user.Address,
                Email = user.Email!,
                Phone = user.PhoneNumber

            };
            logger.LogInformation("Get profile operation completed.");
            return res;
        }
    }
}
