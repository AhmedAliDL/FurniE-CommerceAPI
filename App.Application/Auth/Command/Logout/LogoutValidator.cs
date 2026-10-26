using FluentValidation;

namespace App.Application.Auth.Command.Logout
{
    public class LogoutValidator : AbstractValidator<LogoutCommand>
    {
        public LogoutValidator()
        {
            RuleFor(l => l.RefreshToken)
                .NotEmpty()
                .NotNull()
                .WithMessage("Refresh token is required.");
        }
    }
}
