using App.Domain.Interceptors;

namespace App.Domain.Entities
{
    public class CartItem : ISoftDeletable
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public Guid ShoppingCartId { get; set; }
        public int ItemQuantity { get; set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime DeletedAt { get; set; }

        public ShoppingCart ShoppingCart { get; set; } = null!;
        public Product Product { get; set; } = null!;

    }

}
