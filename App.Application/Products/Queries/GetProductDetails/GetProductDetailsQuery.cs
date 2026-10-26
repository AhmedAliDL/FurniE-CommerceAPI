using App.Application.Products.Dto;
using MediatR;

namespace App.Application.Products.Queries.GetProductDetails
{
    public record GetProductDetailsQuery(Guid ProductId) : IRequest<ProductDetailsDto>;
}
