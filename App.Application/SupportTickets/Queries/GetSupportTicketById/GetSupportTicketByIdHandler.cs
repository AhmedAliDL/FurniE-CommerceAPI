using App.Application.Common.Authorization;
using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.SupportTickets.Dto;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.SupportTickets.Queries.GetSupportTicketById
{
    public class GetSupportTicketByIdHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IIdentityService identityService, IRoleService roleService, ILogger<GetSupportTicketByIdHandler> logger) : IRequestHandler<GetSupportTicketByIdQuery, SupportTicketDto>
    {
        public async Task<SupportTicketDto> Handle(GetSupportTicketByIdQuery request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Support ticket retrieval operation started.");

            Guid userId = currentUserService.UserId ?? throw new NotFoundException("User not found.");

            var ticket = await unitOfWork.SupportTickets.FindAsync(t => t.TicketId == request.TicketId, cancellationToken)
                ?? throw new NotFoundException($"Support ticket with id {request.TicketId} was not found.");

            if (ticket.CustomerId != userId)
            {
                var user = await identityService.GetUserByIdAsync(userId);
                var roles = user == null ? [] : await roleService.GetAllRolesOfUser(user);
                if (!SupportRoles.IsSupportStaff(roles))
                    throw new ForbiddenException("You are not allowed to view this support ticket.");
            }

            logger.LogInformation("Support ticket retrieval operation completed. TicketId: {TicketId}", ticket.TicketId);
            return ToDto(ticket);
        }

        private static SupportTicketDto ToDto(Domain.Entities.SupportTicket t) => new()
        {
            TicketId = t.TicketId,
            Subject = t.Subject,
            Status = t.Status.ToString(),
            Priority = t.Priority.ToString(),
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt ?? t.CreatedAt,
            ClosedAt = t.ClosedAt
        };
    }
}
