using MediatR;

namespace App.Application.Products.Command.DeleteProduct
{
    public record DeleteProductCommand(Guid ProductId) : IRequest<bool>;
}
