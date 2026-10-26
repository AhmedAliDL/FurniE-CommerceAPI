using FluentValidation;

namespace App.Application.Auth.Command.SendEmailConfirmation
{
    public class SendEmailConfirmationValidator : AbstractValidator<SendEmailConfirmationCommand>
    {
        public SendEmailConfirmationValidator()
        {
            RuleFor(e => e.Email)
                .EmailAddress()
                .WithMessage("Email format is not valid.");
        }
    }
}
