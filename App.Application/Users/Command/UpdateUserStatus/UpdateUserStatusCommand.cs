using MediatR;

namespace App.Application.Users.Command.UpdateUserStatus
{
    public record UpdateUserStatusCommand : IRequest<bool>
    {
        public Guid UserId { get; set; }
        public bool IsActive { get; set; }
    }
}
