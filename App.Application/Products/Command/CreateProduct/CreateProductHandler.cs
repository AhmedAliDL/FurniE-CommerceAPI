using App.Application.Common.Interfaces.UnitOfWork;
using App.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Products.Command.CreateProduct
{
    public class CreateProductHandler(IUnitOfWork unitOfWork, ILogger<CreateProductHandler> logger) : IRequestHandler<CreateProductCommand, Guid>
    {
        public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Create product operation started.");
            var product = new Product
            {
                ProductName = request.ProductName,
                Description = request.Description,
                Price = request.Price,
                StockQuantity = request.StockQuantity,
                CategoryId = request.CategoryId,
                ImageUrl = request.ImageUrl,
                IsActive = request.IsActive
            };
            await unitOfWork.Products.AddAsync(product, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);
            logger.LogInformation("Create product operation completed.");
            return product.ProductId;
        }
    }
}
