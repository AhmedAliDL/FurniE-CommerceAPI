using App.Domain.Interceptors;
using System.ComponentModel.DataAnnotations;

namespace App.Domain.Entities
{
    public class Product : ISoftDeletable
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public Guid CategoryId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime DeletedAt { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = null!;
        public Category Category { get; set; } = null!;

        public ICollection<CartItem> CartItems { get; set; } = null!;
        public ICollection<Review> Reviews { get; set; } = null!;
        public ICollection<OrderItem> OrderItems { get; set; } = null!;
        public ICollection<ProductImage> ProductImages { get; set; } = null!;


    }

}
