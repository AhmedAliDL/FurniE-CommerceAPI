using App.Application.Products.Dto;
using MediatR;

namespace App.Application.Products.Queries.GetProductById
{
    public record GetProductByIdQuery(Guid ProductId) : IRequest<ProductResponse>;
}
