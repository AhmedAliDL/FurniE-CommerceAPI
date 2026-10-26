using Microsoft.AspNetCore.Identity;

namespace App.Domain.Entities
{
    public class AppUser : IdentityUser<Guid>
    {
        public string FName { get; set; } = string.Empty;
        public string LName { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<Review> Reviews { get; set; } = null!;
        public ShoppingCart ShoppingCart { get; set; } = null!;
        public ICollection<Order> Orders { get; set; } = null!;
        public ICollection<Notification> SenderNotifications { get; set; } = null!;
        public ICollection<Notification> RecieverNotifications { get; set; } = null!;
        public ICollection<AuditLog> AuditLogs { get; set; } = null!;
        public ICollection<RefreshToken> RefreshTokens { get; set; } = null!;
        public ICollection<WishlistItem> WishlistItems { get; set; } = null!;
        public ICollection<IdempotencyRecord> IdempotencyRecords { get; set; } = null!;



    }
}
