namespace App.Application.ShoppingCarts.Dto
{
    public class ShoppingCartDto
    {
        public Guid ShoppingCartId { get; set; }
        public List<CartItemDto> CartItems { get; set; } = [];
        public decimal TotalPrice { get; set; }
    }
}
