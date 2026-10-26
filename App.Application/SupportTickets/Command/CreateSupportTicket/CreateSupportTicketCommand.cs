using System.Text.Json.Serialization;
using App.Domain.Enums;
using MediatR;

namespace App.Application.SupportTickets.Command.CreateSupportTicket
{
    public record CreateSupportTicketCommand : IRequest<Guid>
    {
        public CreateSupportTicketCommand()
        {
        }

        public CreateSupportTicketCommand(string subject, SupportTicketPriority priority = SupportTicketPriority.Medium)
        {
            Subject = subject;
            Priority = priority;
        }

        public string Subject { get; set; } = null!;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public SupportTicketPriority Priority { get; set; } = SupportTicketPriority.Medium;
    }
}
