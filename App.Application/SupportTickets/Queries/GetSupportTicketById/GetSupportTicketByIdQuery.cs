using App.Application.SupportTickets.Dto;
using MediatR;

namespace App.Application.SupportTickets.Queries.GetSupportTicketById
{
    public record GetSupportTicketByIdQuery(Guid TicketId) : IRequest<SupportTicketDto>;
}
