using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.UnitOfWork;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Products.Command.DeleteProduct
{
    public class DeleteProductHandler(IUnitOfWork unitOfWork, ILogger<DeleteProductHandler> logger) : IRequestHandler<DeleteProductCommand, bool>
    {
        public async Task<bool> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Delete product {ProductId} operation started.", request.ProductId);

            var product = await unitOfWork.Products.FindAsync(p => p.ProductId == request.ProductId, cancellationToken);
            if (product == null)
                return false;

            var orderItem = await unitOfWork.OrderItems.FindAsync(oi => oi.ProductId == request.ProductId, cancellationToken);
            if (orderItem != null)
                throw new ConflictException("Product cannot be deleted because it has been ordered. Deactivate it instead.");

            unitOfWork.Products.Delete(product);
            await unitOfWork.CompleteAsync(cancellationToken);
            logger.LogInformation("Delete product {ProductId} operation completed.", request.ProductId);
            return true;
        }
    }
}
