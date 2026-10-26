using App.Application.Common.Authorization;
using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Roles.Command.DeleteRole
{
    public class DeleteRoleHandler(IRoleService roleService, IIdentityService identityService, ILogger<DeleteRoleHandler> logger) : IRequestHandler<DeleteRoleCommand, bool>
    {
        public async Task<bool> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Delete role {RoleName} operation started.", request.RoleName);

            var role = await roleService.GetRoleAsync(request.RoleName);
            if (role == null)
                return false;

            var roleName = role.Name ?? request.RoleName;
            if (IsBuiltInRole(roleName))
                throw new ConflictException($"Role {roleName} is a built-in role and cannot be deleted.");

            var users = await identityService.GetUsersInRoleAsync(roleName);
            if (users.Count > 0)
                throw new ConflictException($"Role {roleName} is assigned to {users.Count} user(s) and cannot be deleted.");

            var deleted = await roleService.DeleteRoleAsync(role);
            if (!deleted)
                throw new InvalidOperationException($"Failed to delete role {roleName}.");

            logger.LogInformation("Delete role {RoleName} operation completed.", roleName);
            return true;
        }

        private static bool IsBuiltInRole(string roleName)
        {
            return string.Equals(roleName, SupportRoles.Admin, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(roleName, SupportRoles.Customer, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(roleName, SupportRoles.SupportAgent, StringComparison.OrdinalIgnoreCase);
        }
    }
}
