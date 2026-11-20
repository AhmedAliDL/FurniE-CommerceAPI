using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.UnitOfWork;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Application.Products.Command.SetProductStock
{
    public class SetProductStockHandler(IUnitOfWork unitOfWork, ILogger<SetProductStockHandler> logger) : IRequestHandler<SetProductStockCommand, bool>
    {
        public async Task<bool> Handle(SetProductStockCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Set product {ProductId} stock operation started. Quantity: {Quantity}", request.ProductId, request.Quantity);

            var product = await unitOfWork.Products.GetByIdAsync(request.ProductId);
            if (product is null)
            {
                logger.LogWarning("Product {ProductId} was not found.", request.ProductId);
                return false;
            }

            // Stock and visibility move together, matching the checkout and cancellation behavior.
            product.StockQuantity = request.Quantity;
            product.IsActive = request.Quantity > 0;
            unitOfWork.Products.Update(product);

            try
            {
                await unitOfWork.CompleteAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConflictException("Stock was changed concurrently. Retry the request.");
            }

            logger.LogInformation("Set product {ProductId} stock operation completed.", request.ProductId);
            return true;
        }
    }
}
