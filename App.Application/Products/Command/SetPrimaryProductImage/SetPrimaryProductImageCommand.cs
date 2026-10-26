using App.Application.Responses;
using MediatR;

namespace App.Application.Products.Command.SetPrimaryProductImage
{
    public record SetPrimaryProductImageCommand(Guid ProductId, Guid ImageId) : IRequest<ResponseResult>;
}
