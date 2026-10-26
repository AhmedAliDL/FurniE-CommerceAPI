using MediatR;

namespace App.Application.Products.Command.CreateProduct
{
    public record CreateProductCommand : IRequest<Guid>
    {
        public string ProductName { get; set; } = null!;
        public string Description { get; set; } = null!;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public Guid CategoryId { get; set; }
        public string ImageUrl { get; set; } = null!;
        public bool IsActive { get; set; }
    }
}
