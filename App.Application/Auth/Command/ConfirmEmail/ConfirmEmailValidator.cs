using FluentValidation;

namespace App.Application.Auth.Command.ConfirmEmail
{
    public class ConfirmEmailValidator : AbstractValidator<ConfirmEmailCommand>
    {
        public ConfirmEmailValidator()
        {
            RuleFor(e => e.UserId)
                .NotNull()
                .NotEmpty()
                .WithMessage("User id is required.");
            RuleFor(e => e.ConfirmationToken)
                .NotNull()
                .NotEmpty()
                .WithMessage("Token is required.");

        }
    }
}
