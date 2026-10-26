using MediatR;

namespace App.Application.Roles.Command.DeleteRole
{
    public record DeleteRoleCommand(string RoleName) : IRequest<bool>;
}
