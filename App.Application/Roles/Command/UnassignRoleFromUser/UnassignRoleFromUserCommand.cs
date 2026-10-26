using MediatR;

namespace App.Application.Roles.Command.UnassignRoleFromUser
{
    public record UnassignRoleFromUserCommand : IRequest
    {
        public string RoleName { get; set; } = null!;
        public string UserEmail { get; set; } = null!;
    }
}
