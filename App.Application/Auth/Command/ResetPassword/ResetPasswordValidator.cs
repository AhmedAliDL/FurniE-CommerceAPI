using FluentValidation;

namespace App.Application.Auth.Command.ResetPassword
{
    public class ResetPasswordValidator : AbstractValidator<ResetPasswordCommand>
    {
        public ResetPasswordValidator()
        {
            RuleFor(p => p.ConfirmationToken)
                .NotEmpty()
                .NotNull()
                .WithMessage("Token is required.");
            RuleFor(p => p.ConfirmNewPassword)
                .Equal(p => p.NewPassword)
                .WithMessage("Confirm Password must be equal to new password.");
        }
    }
}
