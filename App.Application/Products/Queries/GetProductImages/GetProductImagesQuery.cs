using App.Application.Products.Dto;
using App.Application.Responses;
using MediatR;

namespace App.Application.Products.Queries.GetProductImages
{
    public record GetProductImagesQuery(Guid ProductId) : IRequest<ResponseResult<List<ProductImageDto>>>;
}
