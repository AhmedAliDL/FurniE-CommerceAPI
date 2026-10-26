namespace App.Application.Orders.Dto
{
    public class ReturnItemRequestDto
    {
        public Guid OrderItemId { get; set; }
        public int Quantity { get; set; }
    }
}
