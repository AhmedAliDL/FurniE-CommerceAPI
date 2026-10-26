using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Responses;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Products.Command.SetPrimaryProductImage
{
    public class SetPrimaryProductImageHandler(IUnitOfWork unitOfWork, ILogger<SetPrimaryProductImageHandler> logger) : IRequestHandler<SetPrimaryProductImageCommand, ResponseResult>
    {
        public async Task<ResponseResult> Handle(SetPrimaryProductImageCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Set primary product image operation started. ProductId: {ProductId}, ImageId: {ImageId}", request.ProductId, request.ImageId);

            var product = await unitOfWork.Products.FindAsync(p => p.ProductId == request.ProductId, cancellationToken)
                ?? throw new NotFoundException($"Product with id {request.ProductId} was not found.");

            var target = await unitOfWork.ProductImages.FindAsync(i => i.Id == request.ImageId && i.ProductId == product.ProductId, cancellationToken)
                ?? throw new NotFoundException($"Image with id {request.ImageId} was not found for this product.");

            if (target.IsPrimary)
                return ResponseResult.Success();

            var siblings = await unitOfWork.ProductImages.FindAllAsync(i => i.ProductId == product.ProductId && i.IsPrimary, cancellationToken);
            foreach (var sibling in siblings)
            {
                sibling.IsPrimary = false;
                sibling.UpdatedAt = DateTime.UtcNow;
                unitOfWork.ProductImages.Update(sibling);
            }

            // The filtered unique index allows one primary per product; persist the
            // old primary as non-primary before promoting the target.
            await unitOfWork.CompleteAsync(cancellationToken);

            target.IsPrimary = true;
            target.UpdatedAt = DateTime.UtcNow;
            unitOfWork.ProductImages.Update(target);

            if (product.ImageUrl != target.ImageUrl)
            {
                product.ImageUrl = target.ImageUrl;
                product.UpdatedAt = DateTime.UtcNow;
                unitOfWork.Products.Update(product);
            }

            await unitOfWork.CompleteAsync(cancellationToken);

            logger.LogInformation("Set primary product image operation completed. ProductId: {ProductId}, ImageId: {ImageId}", product.ProductId, target.Id);
            return ResponseResult.Success();
        }
    }
}
