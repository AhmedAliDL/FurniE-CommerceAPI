using FluentValidation;

namespace App.Application.SupportChat.Command.CloseConversation
{
    public class CloseConversationValidator : AbstractValidator<CloseConversationCommand>
    {
        public CloseConversationValidator()
        {
            RuleFor(c => c.TicketId)
                .NotEmpty()
                .WithMessage("Ticket id is required.");
        }
    }
}
