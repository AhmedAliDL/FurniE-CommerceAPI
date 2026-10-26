namespace App.Application.Orders.Dto
{
    public class OrderDetailsDto
    {
        public Guid OrderId { get; set; }
        public Guid OrderItemId { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductImage { get; set; } = string.Empty;
        public decimal ProductPrice { get; set; }
        public double ProductQuantity { get; set; }
        public string ShippingStatus { get; set; } = string.Empty;
        public bool IsReviewed { get; set; }
    }
}
