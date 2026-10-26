using App.Application.Common.Interfaces.Services;
using App.Application.Roles.Dto;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Roles.Queries.GetRoles
{
    public class GetRolesHandler(IRoleService roleService, ILogger<GetRolesHandler> logger) : IRequestHandler<GetRolesQuery, List<RoleDto>>
    {
        public async Task<List<RoleDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Get all roles operation started.");

            var roles = await roleService.GetAllRolesAsync();
            var roleDtos = roles.Select(r => new RoleDto
            {
                RoleName = r.Name ?? string.Empty,
                RoleDescription = string.Empty
            }).ToList();

            logger.LogInformation("Get all roles operation completed. {Count} roles returned.", roleDtos.Count);
            return roleDtos;
        }
    }
}
