using App.Application.Common.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Roles.Command.CreateRole
{
    public class CreateRoleHandler(IRoleService roleService, ILogger<CreateRoleHandler> logger) : IRequestHandler<CreateRoleCommand>
    {
        public async Task Handle(CreateRoleCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Create role {RoleName} operation started.", request.RoleName);

            if (await roleService.IsRoleExistsAsync(request.RoleName))
                throw new InvalidOperationException($"Role {request.RoleName} already exists.");

            var result = await roleService.CreateRoleAsync(request.RoleName, request.RoleDescription);
            if (!result.Succeeded)
                throw new InvalidOperationException($"Failed to create role {request.RoleName}.");

            logger.LogInformation("Create role {RoleName} operation completed.", request.RoleName);
        }
    }
}
