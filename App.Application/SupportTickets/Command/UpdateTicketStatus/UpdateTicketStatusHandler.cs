using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.SupportTickets.Command.UpdateTicketStatus
{
    public class UpdateTicketStatusHandler(IUnitOfWork unitOfWork, ILogger<UpdateTicketStatusHandler> logger) : IRequestHandler<UpdateTicketStatusCommand>
    {
        public async Task Handle(UpdateTicketStatusCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Support ticket status update operation started.");

            var ticket = await unitOfWork.SupportTickets.FindAsync(t => t.TicketId == request.TicketId, cancellationToken)
                ?? throw new NotFoundException($"Support ticket with id {request.TicketId} was not found.");

            if (ticket.Status == SupportTicketStatus.Closed && request.Status != SupportTicketStatus.Open)
                throw new ConflictException("A closed support ticket can only be reopened by setting its status to Open.");

            ticket.Status = request.Status;
            ticket.UpdatedAt = DateTime.UtcNow;
            ticket.ClosedAt = request.Status == SupportTicketStatus.Closed ? DateTime.UtcNow : null;

            unitOfWork.SupportTickets.Update(ticket);
            await unitOfWork.CompleteAsync(cancellationToken);

            logger.LogInformation("Support ticket status update operation completed. TicketId: {TicketId}, Status: {Status}", ticket.TicketId, ticket.Status);
        }
    }
}
