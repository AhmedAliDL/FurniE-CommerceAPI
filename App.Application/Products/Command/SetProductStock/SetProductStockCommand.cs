using MediatR;

namespace App.Application.Products.Command.SetProductStock
{
    public record SetProductStockCommand : IRequest<bool>
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
