using FluentValidation;

namespace App.Application.SupportChat.Command.ReopenConversation
{
    public class ReopenConversationValidator : AbstractValidator<ReopenConversationCommand>
    {
        public ReopenConversationValidator()
        {
            RuleFor(c => c.TicketId)
                .NotEmpty()
                .WithMessage("Ticket id is required.");
        }
    }
}
