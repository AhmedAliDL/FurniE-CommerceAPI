using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Products.Dto;
using App.Application.Responses;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Products.Queries.GetProductImages
{
    public class GetProductImagesHandler(IUnitOfWork unitOfWork, ILogger<GetProductImagesHandler> logger) : IRequestHandler<GetProductImagesQuery, ResponseResult<List<ProductImageDto>>>
    {
        public async Task<ResponseResult<List<ProductImageDto>>> Handle(GetProductImagesQuery request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Get product images operation started. ProductId: {ProductId}", request.ProductId);

            var productExists = await unitOfWork.Products.FindAsync(p => p.ProductId == request.ProductId, cancellationToken) != null
                ? true
                : throw new NotFoundException($"Product with id {request.ProductId} was not found.");

            var images = await unitOfWork.ProductImages.FindAllAsync(i => i.ProductId == request.ProductId, cancellationToken);

            var items = images
                .OrderByDescending(i => i.IsPrimary)
                .ThenBy(i => i.SortOrder)
                .Select(i => new ProductImageDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ImageUrl = i.ImageUrl,
                    IsPrimary = i.IsPrimary,
                    SortOrder = i.SortOrder
                })
                .ToList();

            logger.LogInformation("Get product images operation completed. ProductId: {ProductId}, Count: {Count}", request.ProductId, items.Count);
            return ResponseResult<List<ProductImageDto>>.Success(items);
        }
    }
}
