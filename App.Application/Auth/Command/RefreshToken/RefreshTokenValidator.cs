using FluentValidation;

namespace App.Application.Auth.Command.RefreshToken
{
    public class RefreshTokenValidator : AbstractValidator<RefreshTokenCommand>
    {
        public RefreshTokenValidator()
        {
            RuleFor(l => l.RefreshToken)
              .NotEmpty()
              .NotNull()
              .WithMessage("Refresh token is required.");
        }
    }
}
