using App.Application.Responses;
using MediatR;

namespace App.Application.Products.Command.ReorderProductImages
{
    public record ReorderProductImagesCommand(Guid ProductId, IReadOnlyList<Guid> ImageIds) : IRequest<ResponseResult>;
}
