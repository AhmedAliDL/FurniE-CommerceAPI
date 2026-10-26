using App.Domain.Enums;
using FluentValidation;

namespace App.Application.SupportTickets.Command.CreateSupportTicket
{
    public class CreateSupportTicketValidator : AbstractValidator<CreateSupportTicketCommand>
    {
        public CreateSupportTicketValidator()
        {
            RuleFor(x => x.Subject)
                .NotEmpty()
                .MaximumLength(200);

            RuleFor(x => x.Priority)
                .IsInEnum();
        }
    }
}
