using MediatR;

namespace App.Application.SupportChat.Queries.CheckTicketAccess
{
    public record CheckTicketAccessQuery(Guid TicketId, Guid UserId) : IRequest;
}
