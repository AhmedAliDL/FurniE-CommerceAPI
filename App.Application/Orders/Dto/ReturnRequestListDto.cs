using App.Domain.Enums;

namespace App.Application.Orders.Dto
{
    public class ReturnRequestListDto
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public string? CustomerName { get; set; }
        public string ReasonCategory { get; set; } = string.Empty;
        public string ReasonText { get; set; } = string.Empty;
        public ReturnRequestStatus Status { get; set; }
        public string? CourierName { get; set; }
        public string? CourierPhone { get; set; }
        public DateTime? ReceivedAt { get; set; }
        public long? RefundAmountCents { get; set; }
        public string? AdminNote { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public List<ReturnItemListDto> Items { get; set; } = [];
    }

    public class ReturnItemListDto
    {
        public Guid Id { get; set; }
        public Guid OrderItemId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}
