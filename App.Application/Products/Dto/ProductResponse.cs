namespace App.Application.Products.Dto
{
    public sealed class ProductResponse
    {
        public Guid ProductId { get; init; }
        public string ProductName { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public decimal Price { get; init; }
        public int StockQuantity { get; init; }
        public Guid CategoryId { get; init; }
        public string CategoryName { get; init; } = string.Empty;
        public string ImageUrl { get; init; } = string.Empty;
        public bool IsAvailable { get; init; }
        public bool IsActive { get; init; }
    }
}
