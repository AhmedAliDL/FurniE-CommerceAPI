using MediatR;

namespace App.Application.ShoppingCarts.Command.UpdateCartQuantities
{
    public record UpdateCartQuantitiesCommand : IRequest
    {
        public Guid ShoppingCartId { get; set; }

        public List<CartItemQuantity> Items { get; set; } = [];
    }

    public record CartItemQuantity
    {
        public Guid ProductId { get; set; }

        public int Quantity { get; set; }
    }
}
