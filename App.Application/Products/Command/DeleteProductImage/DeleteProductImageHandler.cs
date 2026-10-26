using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Responses;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Products.Command.DeleteProductImage
{
    public class DeleteProductImageHandler(IUnitOfWork unitOfWork, IFileStorageService fileStorageService, ILogger<DeleteProductImageHandler> logger) : IRequestHandler<DeleteProductImageCommand, ResponseResult>
    {
        public async Task<ResponseResult> Handle(DeleteProductImageCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Delete product image operation started. ProductId: {ProductId}, ImageId: {ImageId}", request.ProductId, request.ImageId);

            var product = await unitOfWork.Products.FindAsync(p => p.ProductId == request.ProductId, cancellationToken)
                ?? throw new NotFoundException($"Product with id {request.ProductId} was not found.");

            var image = await unitOfWork.ProductImages.FindAsync(i => i.Id == request.ImageId && i.ProductId == product.ProductId, cancellationToken)
                ?? throw new NotFoundException($"Image with id {request.ImageId} was not found for this product.");

            bool wasPrimary = image.IsPrimary;
            string imageUrl = image.ImageUrl;
            unitOfWork.ProductImages.Delete(image);

            // The filtered unique index allows one primary per product; persist the
            // soft delete first so the old primary no longer occupies the index slot.
            await unitOfWork.CompleteAsync(cancellationToken);

            if (wasPrimary)
            {
                var remaining = (await unitOfWork.ProductImages.FindAllAsync(i => i.ProductId == product.ProductId, cancellationToken))
                    .OrderBy(i => i.SortOrder)
                    .ToList();

                string? newPrimaryUrl = null;
                if (remaining.Count > 0)
                {
                    var promoted = remaining[0];
                    promoted.IsPrimary = true;
                    promoted.UpdatedAt = DateTime.UtcNow;
                    unitOfWork.ProductImages.Update(promoted);
                    newPrimaryUrl = promoted.ImageUrl;
                }

                if (product.ImageUrl != (newPrimaryUrl ?? string.Empty))
                {
                    product.ImageUrl = newPrimaryUrl ?? string.Empty;
                    product.UpdatedAt = DateTime.UtcNow;
                    unitOfWork.Products.Update(product);
                }

                await unitOfWork.CompleteAsync(cancellationToken);
            }

            await fileStorageService.DeleteAsync(imageUrl, cancellationToken);

            logger.LogInformation("Delete product image operation completed. ProductId: {ProductId}, ImageId: {ImageId}", product.ProductId, request.ImageId);
            return ResponseResult.Success();
        }
    }
}
