using App.Domain.Interceptors;

namespace App.Domain.Entities
{
    public class ShoppingCart : ISoftDeletable
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime DeletedAt { get; set; }

        public AppUser Customer { get; set; } = null!;

        public ICollection<CartItem> ProductsCartItems { get; set; } = null!;
    }

}
