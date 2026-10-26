using FluentValidation;

namespace App.Application.SupportChat.Command.MarkConversationRead
{
    public class MarkConversationReadValidator : AbstractValidator<MarkConversationReadCommand>
    {
        public MarkConversationReadValidator()
        {
            RuleFor(c => c.TicketId)
                .NotEmpty()
                .WithMessage("Ticket id is required.");
        }
    }
}
