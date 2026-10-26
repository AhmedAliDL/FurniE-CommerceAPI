using FluentValidation;

namespace App.Application.SupportChat.Command.SendMessage
{
    public class SendMessageValidator : AbstractValidator<SendMessageCommand>
    {
        public SendMessageValidator()
        {
            RuleFor(m => m.TicketId)
                .NotEmpty()
                .WithMessage("Ticket id is required.");
            RuleFor(m => m.SenderId)
                .NotEmpty()
                .WithMessage("Sender id is required.");
            RuleFor(m => m.Content)
                .NotEmpty()
                .WithMessage("Message content is required.")
                .MaximumLength(2000)
                .WithMessage("Message content must not exceed 2000 characters.");
        }
    }
}
