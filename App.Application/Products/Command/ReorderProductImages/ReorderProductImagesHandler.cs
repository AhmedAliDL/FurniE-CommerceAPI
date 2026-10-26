using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Responses;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Products.Command.ReorderProductImages
{
    public class ReorderProductImagesHandler(IUnitOfWork unitOfWork, ILogger<ReorderProductImagesHandler> logger) : IRequestHandler<ReorderProductImagesCommand, ResponseResult>
    {
        public async Task<ResponseResult> Handle(ReorderProductImagesCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Reorder product images operation started. ProductId: {ProductId}", request.ProductId);

            var product = await unitOfWork.Products.FindAsync(p => p.ProductId == request.ProductId, cancellationToken)
                ?? throw new NotFoundException($"Product with id {request.ProductId} was not found.");

            var images = (await unitOfWork.ProductImages.FindAllAsync(i => i.ProductId == product.ProductId, cancellationToken)).ToList();

            var existingIds = images.Select(i => i.Id).ToHashSet();
            if (request.ImageIds.Count != images.Count || !request.ImageIds.All(existingIds.Contains))
                return ResponseResult.Failure("The image id list must contain exactly this product's images.");

            for (int index = 0; index < request.ImageIds.Count; index++)
            {
                var image = images.First(i => i.Id == request.ImageIds[index]);
                if (image.SortOrder == index)
                    continue;

                image.SortOrder = index;
                image.UpdatedAt = DateTime.UtcNow;
                unitOfWork.ProductImages.Update(image);
            }

            await unitOfWork.CompleteAsync(cancellationToken);

            logger.LogInformation("Reorder product images operation completed. ProductId: {ProductId}, Count: {Count}", product.ProductId, images.Count);
            return ResponseResult.Success();
        }
    }
}
