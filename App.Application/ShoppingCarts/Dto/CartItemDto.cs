namespace App.Application.ShoppingCarts.Dto
{
    public class CartItemDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int ItemQuantity { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
