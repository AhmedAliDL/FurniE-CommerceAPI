using App.Domain.Enums;

namespace App.Application.Orders.Queries.GetCancellationRequests
{
    public class CancellationRequestListDto
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public string? CustomerName { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string PreviousOrderStatus { get; set; } = string.Empty;
        public CancellationRequestStatus Status { get; set; }
        public string? AdminNote { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }
}
