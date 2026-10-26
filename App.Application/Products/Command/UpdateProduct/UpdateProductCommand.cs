using MediatR;

namespace App.Application.Products.Command.UpdateProduct
{
    public record UpdateProductCommand : IRequest<bool>
    {
        public Guid ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public int? StockQuantity { get; set; }
        public Guid? CategoryId { get; set; }
        public string? ImageUrl { get; set; }
        public bool? IsActive { get; set; }
    }
}
