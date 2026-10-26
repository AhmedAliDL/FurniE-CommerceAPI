using App.Application.Responses;
using App.Application.SupportTickets.Dto;
using App.Domain.Enums;
using MediatR;

namespace App.Application.SupportTickets.Queries.GetAllTickets
{
    public record GetAllTicketsQuery(SupportTicketStatus? Status = null, int Page = 1, int PageSize = 20) : IRequest<PagedResult<AdminSupportTicketDto>>;
}
