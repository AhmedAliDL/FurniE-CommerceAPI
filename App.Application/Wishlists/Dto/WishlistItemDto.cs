namespace App.Application.Wishlists.Dto
{
    public sealed class WishlistItemDto
    {
        public Guid ProductId { get; init; }
        public string ProductName { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public decimal Price { get; init; }
        public string ImageUrl { get; init; } = string.Empty;
        public bool IsAvailable { get; init; }
        public bool IsActive { get; init; }
        public DateTime AddedAt { get; init; }
    }
}
