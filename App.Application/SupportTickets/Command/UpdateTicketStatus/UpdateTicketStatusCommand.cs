using System.Text.Json.Serialization;
using App.Domain.Enums;
using MediatR;

namespace App.Application.SupportTickets.Command.UpdateTicketStatus
{
    public record UpdateTicketStatusCommand : IRequest
    {
        public UpdateTicketStatusCommand()
        {
        }

        public UpdateTicketStatusCommand(Guid ticketId, SupportTicketStatus status)
        {
            TicketId = ticketId;
            Status = status;
        }

        public Guid TicketId { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public SupportTicketStatus Status { get; set; }
    }
}
