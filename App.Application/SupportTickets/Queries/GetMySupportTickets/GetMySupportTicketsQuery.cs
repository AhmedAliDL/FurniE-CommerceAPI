using App.Application.Responses;
using App.Application.SupportTickets.Dto;
using MediatR;

namespace App.Application.SupportTickets.Queries.GetMySupportTickets
{
    public record GetMySupportTicketsQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<SupportTicketDto>>;
}
