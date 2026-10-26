using FluentValidation;

namespace App.Application.Auth.Command.ForgetPassword
{
    public class ForgetPasswordValidator : AbstractValidator<ForgetPasswordCommand>
    {
        public ForgetPasswordValidator()
        {
            RuleFor(p => p.Email)
                .EmailAddress()
                .WithMessage("Email format is not valid.");
        }
    }
}
