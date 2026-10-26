using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Products.Dto;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Products.Queries.GetProductById
{
    public class GetProductByIdHandler(IUnitOfWork unitOfWork, ILogger<GetProductByIdHandler> logger) : IRequestHandler<GetProductByIdQuery, ProductResponse>
    {
        public async Task<ProductResponse> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Get product {ProductId} operation started.", request.ProductId);

            var product = await unitOfWork.Products.FindAsync(p => p.ProductId == request.ProductId, [p => p.Category], cancellationToken)
                ?? throw new NotFoundException($"Product with id {request.ProductId} was not found.");

            logger.LogInformation("Get product {ProductId} operation completed.", request.ProductId);
            return new ProductResponse
            {
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                Description = product.Description,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.CategoryName ?? string.Empty,
                ImageUrl = product.ImageUrl,
                IsAvailable = product.StockQuantity > 0,
                IsActive = product.IsActive
            };
        }
    }
}
