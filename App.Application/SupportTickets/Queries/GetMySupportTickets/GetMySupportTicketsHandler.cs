using App.Application.Common.Enums;
using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Responses;
using App.Application.SupportTickets.Dto;
using App.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.SupportTickets.Queries.GetMySupportTickets
{
    public class GetMySupportTicketsHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, ILogger<GetMySupportTicketsHandler> logger) : IRequestHandler<GetMySupportTicketsQuery, PagedResult<SupportTicketDto>>
    {
        public async Task<PagedResult<SupportTicketDto>> Handle(GetMySupportTicketsQuery request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Support tickets retrieval operation started.");

            Guid userId = currentUserService.UserId ?? throw new NotFoundException("User not found.");

            (IReadOnlyList<SupportTicket> tickets, int totalCount) = await unitOfWork.SupportTickets.FindPagedAsync(
                t => t.CustomerId == userId,
                [],
                SortDirection.Descending,
                t => t.CreatedAt,
                request.Page,
                request.PageSize,
                cancellationToken);

            var items = tickets.Select(ToDto).ToList();

            logger.LogInformation("Support tickets retrieval operation completed. TicketCount: {TicketCount}", totalCount);
            return new PagedResult<SupportTicketDto>
            {
                Items = items,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }

        private static SupportTicketDto ToDto(SupportTicket t) => new()
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
