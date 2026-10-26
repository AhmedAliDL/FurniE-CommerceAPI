namespace App.Application.Orders.Dto
{
    public class OrderShippingDto
    {
        public Guid OrderId { get; set; }
        public string Address { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string ShippingStatus { get; set; } = string.Empty;
        public int TotalQuantity { get; set; }
        public decimal TotalPrice { get; set; }
        public int RemainingDays { get; set; }
    }
}
