using MediatR;

namespace App.Application.Roles.Command.CreateRole
{
    public record CreateRoleCommand : IRequest
    {
        public string RoleName { get; set; } = null!;
        public string RoleDescription { get; set; } = string.Empty;
    }
}
