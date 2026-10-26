using App.Domain.Enums;
using FluentValidation;

namespace App.Application.SupportTickets.Command.UpdateTicketStatus
{
    public class UpdateTicketStatusValidator : AbstractValidator<UpdateTicketStatusCommand>
    {
        public UpdateTicketStatusValidator()
        {
            RuleFor(x => x.TicketId)
                .NotEmpty();

            RuleFor(x => x.Status)
                .IsInEnum();
        }
    }
}
