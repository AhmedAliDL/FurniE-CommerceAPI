using App.Application.Responses;
using MediatR;

namespace App.Application.Products.Command.DeleteProductImage
{
    public record DeleteProductImageCommand(Guid ProductId, Guid ImageId) : IRequest<ResponseResult>;
}
