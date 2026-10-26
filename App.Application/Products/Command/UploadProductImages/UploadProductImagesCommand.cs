using App.Application.Products.Dto;
using App.Application.Responses;
using MediatR;

namespace App.Application.Products.Command.UploadProductImages
{
    public record UploadProductImagesCommand : IRequest<ResponseResult<List<ProductImageDto>>>
    {
        public Guid ProductId { get; init; }
        public IReadOnlyList<ImageFileUpload> Files { get; init; } = [];
    }
}
