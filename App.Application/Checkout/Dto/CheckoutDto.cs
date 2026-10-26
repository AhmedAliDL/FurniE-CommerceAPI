using App.Application.ShoppingCarts.Dto;

namespace App.Application.Checkout.Dto
{
    public class CheckoutDto
    {
        public Guid ShoppingCartId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public List<CartItemDto> CartItems { get; set; } = [];
        public decimal TotalPrice { get; set; }
    }
}
