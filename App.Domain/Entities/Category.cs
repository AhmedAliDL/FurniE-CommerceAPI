using App.Domain.Interceptors;

namespace App.Domain.Entities
{
    public class Category : ISoftDeletable
    {
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime DeletedAt { get; set; }

        public ICollection<Product> Products { get; set; } = null!;

    }

}
