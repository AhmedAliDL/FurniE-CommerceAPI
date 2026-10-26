using MediatR;

namespace App.Application.Auth.Command.ResetPassword
{
    public record ResetPasswordCommand : IRequest<bool>
    {
        public string ConfirmationToken { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmNewPassword { get; set; } = string.Empty;

    }
}
