using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Roles.Command.UnassignRoleFromUser
{
    public class UnassignRoleFromUserHandler(IIdentityService identityService, IRoleService roleService, ILogger<UnassignRoleFromUserHandler> logger)
        : IRequestHandler<UnassignRoleFromUserCommand>
    {
        public async Task Handle(UnassignRoleFromUserCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Unassign role {RoleName} from user {UserEmail} operation started.", request.RoleName, request.UserEmail);

            var user = await identityService.GetUserByEmailAsync(request.UserEmail)
                ?? throw new NotFoundException($"User with email {request.UserEmail} was not found.");

            if (!await roleService.IsRoleExistsAsync(request.RoleName))
                throw new InvalidRoleException($"Role {request.RoleName} does not exist.");

            var userRoles = await roleService.GetAllRolesOfUser(user);
            if (!userRoles.Contains(request.RoleName, StringComparer.OrdinalIgnoreCase))
            {
                logger.LogInformation("User {UserEmail} does not have role {RoleName}; nothing to unassign.", request.UserEmail, request.RoleName);
                return;
            }

            var result = await roleService.DeleteAssignRoleAsync(user, request.RoleName);
            if (!result)
                throw new InvalidOperationException($"Failed to unassign role {request.RoleName} from user {request.UserEmail}.");

            logger.LogInformation("Unassign role {RoleName} from user {UserEmail} operation completed.", request.RoleName, request.UserEmail);
        }
    }
}
