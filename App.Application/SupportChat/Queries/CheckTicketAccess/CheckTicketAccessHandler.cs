using App.Application.Common.Authorization;
using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.SupportChat.Queries.CheckTicketAccess
{
    public class CheckTicketAccessHandler(IUnitOfWork unitOfWork, IIdentityService identityService, IRoleService roleService, ILogger<CheckTicketAccessHandler> logger) : IRequestHandler<CheckTicketAccessQuery>
    {
        public async Task Handle(CheckTicketAccessQuery request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Support ticket access check operation started.");

            var ticket = await unitOfWork.SupportTickets.FindAsync(t => t.TicketId == request.TicketId, cancellationToken)
                ?? throw new NotFoundException($"Support ticket with id {request.TicketId} was not found.");

            if (ticket.CustomerId != request.UserId)
            {
                var user = await identityService.GetUserByIdAsync(request.UserId);
                var roles = user == null ? [] : await roleService.GetAllRolesOfUser(user);
                if (!SupportRoles.IsSupportStaff(roles))
                    throw new ForbiddenException("You are not allowed to access this support ticket.");
            }

            logger.LogInformation("Support ticket access check operation completed. TicketId: {TicketId}", ticket.TicketId);
        }
    }
}
