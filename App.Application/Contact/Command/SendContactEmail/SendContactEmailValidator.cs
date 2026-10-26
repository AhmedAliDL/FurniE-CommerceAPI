using FluentValidation;

namespace App.Application.Contact.Command.SendContactEmail
{
    public class SendContactEmailValidator : AbstractValidator<SendContactEmailCommand>
    {
        public SendContactEmailValidator()
        {
            RuleFor(c => c.Subject)
                .NotEmpty()
                .Length(3, 40)
                .WithMessage("Subject must be between 3 and 40 characters.");
            RuleFor(c => c.Body)
                .NotEmpty()
                .MinimumLength(3)
                .WithMessage("Body must be at least 3 characters long.");
        }
    }
}
