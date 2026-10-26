using FluentValidation;

namespace App.Application.Auth.Command.Login
{
    public class LoginValidator : AbstractValidator<LoginCommand>
    {
        public LoginValidator()
        {
            RuleFor(l => l.Email)
                .EmailAddress()
                .WithMessage("Invalid email address.");
        }
    }
}
