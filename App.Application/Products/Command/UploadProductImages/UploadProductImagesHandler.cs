using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Products.Dto;
using App.Application.Responses;
using App.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Products.Command.UploadProductImages
{
    public class UploadProductImagesHandler(IUnitOfWork unitOfWork, IFileStorageService fileStorageService, ILogger<UploadProductImagesHandler> logger) : IRequestHandler<UploadProductImagesCommand, ResponseResult<List<ProductImageDto>>>
    {
        public async Task<ResponseResult<List<ProductImageDto>>> Handle(UploadProductImagesCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Upload product images operation started. ProductId: {ProductId}", request.ProductId);

            var product = await unitOfWork.Products.FindAsync(p => p.ProductId == request.ProductId, cancellationToken)
                ?? throw new NotFoundException($"Product with id {request.ProductId} was not found.");

            var existingImages = (await unitOfWork.ProductImages.FindAllAsync(i => i.ProductId == product.ProductId, cancellationToken)).ToList();
            bool hasPrimary = existingImages.Any(i => i.IsPrimary);
            var nextSortOrder = existingImages.Count == 0 ? 0 : existingImages.Max(i => i.SortOrder) + 1;

            var savedUrls = new List<string>();
            var uploaded = new List<ProductImage>();
            try
            {
                for (int index = 0; index < request.Files.Count; index++)
                {
                    var file = request.Files[index];
                    string url;
                    await using (file.Content)
                    {
                        url = await fileStorageService.SaveAsync(file.FileName, file.Content, cancellationToken);
                    }
                    savedUrls.Add(url);

                    uploaded.Add(new ProductImage
                    {
                        ProductId = product.ProductId,
                        ImageUrl = url,
                        IsPrimary = !hasPrimary && index == 0,
                        SortOrder = nextSortOrder + index
                    });
                }

                await unitOfWork.ProductImages.AddRangeAsync(uploaded, cancellationToken);

                var primary = uploaded.FirstOrDefault(i => i.IsPrimary);
                if (primary != null && product.ImageUrl != primary.ImageUrl)
                {
                    product.ImageUrl = primary.ImageUrl;
                    product.UpdatedAt = DateTime.UtcNow;
                    unitOfWork.Products.Update(product);
                }

                await unitOfWork.CompleteAsync(cancellationToken);
            }
            catch
            {
                foreach (var url in savedUrls)
                {
                    await fileStorageService.DeleteAsync(url, cancellationToken);
                }
                throw;
            }

            logger.LogInformation("Upload product images operation completed. ProductId: {ProductId}, Count: {Count}", product.ProductId, uploaded.Count);
            return ResponseResult<List<ProductImageDto>>.Success(uploaded.Select(ToDto).ToList());
        }

        private static ProductImageDto ToDto(ProductImage image) => new()
        {
            Id = image.Id,
            ProductId = image.ProductId,
            ImageUrl = image.ImageUrl,
            IsPrimary = image.IsPrimary,
            SortOrder = image.SortOrder
        };
    }
}
