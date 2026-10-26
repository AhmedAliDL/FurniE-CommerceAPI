using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Roles.Command.AssignRoleToUser
{
    public class AssignRoleToUserHandler(IIdentityService identityService, IRoleService roleService, ILogger<AssignRoleToUserHandler> logger)
        : IRequestHandler<AssignRoleToUserCommand>
    {
        public async Task Handle(AssignRoleToUserCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Assign role {RoleName} to user {UserEmail} operation started.", request.RoleName, request.UserEmail);

            var user = await identityService.GetUserByEmailAsync(request.UserEmail)
                ?? throw new NotFoundException($"User with email {request.UserEmail} was not found.");

            if (!await roleService.IsRoleExistsAsync(request.RoleName))
                throw new InvalidRoleException($"Role {request.RoleName} does not exist.");

            var result = await roleService.AssignRoleToUserAsync(user, request.RoleName);
            if (!result.Succeeded)
                throw new InvalidOperationException($"Failed to assign role {request.RoleName} to user {request.UserEmail}.");

            logger.LogInformation("Assign role {RoleName} to user {UserEmail} operation completed.", request.RoleName, request.UserEmail);
        }
    }
}
