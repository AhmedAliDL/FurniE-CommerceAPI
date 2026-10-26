using FluentValidation;

namespace App.Application.Auth.Command.ChangePassword
{
    public class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
    {
        public ChangePasswordValidator()
        {
            RuleFor(p => p.ConfirmNewPassword)
                .Equal(p => p.NewPassword)
                .WithMessage("Confirm Password must be equal to new password.");
        }
    }
}
