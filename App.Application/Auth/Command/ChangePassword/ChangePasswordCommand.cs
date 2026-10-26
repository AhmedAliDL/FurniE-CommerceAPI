using MediatR;

namespace App.Application.Auth.Command.ChangePassword
{
    public record ChangePasswordCommand : IRequest<bool>
    {
        public string OldPassword { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
        public string ConfirmNewPassword { get; set; } = null!;

    }

}
