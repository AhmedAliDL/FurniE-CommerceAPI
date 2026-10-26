using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Domain.Entities;
using App.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.SupportTickets.Command.CreateSupportTicket
{
    public class CreateSupportTicketHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, ILogger<CreateSupportTicketHandler> logger) : IRequestHandler<CreateSupportTicketCommand, Guid>
    {
        public async Task<Guid> Handle(CreateSupportTicketCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Support ticket creation operation started.");

            Guid userId = currentUserService.UserId ?? throw new NotFoundException("User not found.");

            var ticket = new SupportTicket
            {
                CustomerId = userId,
                Subject = request.Subject,
                Priority = request.Priority,
                Status = SupportTicketStatus.Open
            };

            await unitOfWork.SupportTickets.AddAsync(ticket, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);

            logger.LogInformation("Support ticket creation operation completed. TicketId: {TicketId}", ticket.TicketId);
            return ticket.TicketId;
        }
    }
}
