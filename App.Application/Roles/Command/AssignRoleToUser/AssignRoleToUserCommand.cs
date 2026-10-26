using MediatR;

namespace App.Application.Roles.Command.AssignRoleToUser
{
    public record AssignRoleToUserCommand : IRequest
    {
        public string RoleName { get; set; } = null!;
        public string UserEmail { get; set; } = null!;
    }
}
