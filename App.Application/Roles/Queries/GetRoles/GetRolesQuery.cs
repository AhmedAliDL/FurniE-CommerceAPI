using App.Application.Roles.Dto;
using MediatR;

namespace App.Application.Roles.Queries.GetRoles
{
    public record GetRolesQuery : IRequest<List<RoleDto>>;
}
