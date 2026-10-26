using App.Domain.Interceptors;

namespace App.Domain.Entities
{
    public class Review : ISoftDeletable
    {
        public Guid ReviewId { get; set; }
        public Guid UserId { get; set; }
        public Guid ProductId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime DeletedAt { get; set; }

        public AppUser Customer { get; set; } = null!;
        public Product Product { get; set; } = null!;

    }

}
