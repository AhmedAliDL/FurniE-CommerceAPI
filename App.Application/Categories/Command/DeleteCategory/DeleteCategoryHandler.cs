using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.UnitOfWork;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Categories.Command.DeleteCategory
{
    public class DeleteCategoryHandler(IUnitOfWork unitOfWork, ILogger<DeleteCategoryHandler> logger) : IRequestHandler<DeleteCategoryCommand, bool>
    {
        public async Task<bool> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Delete category {CategoryId} operation started.", request.CategoryId);

            var category = await unitOfWork.Categories.FindAsync(c => c.CategoryId == request.CategoryId, cancellationToken);
            if (category == null)
                return false;

            var product = await unitOfWork.Products.FindAsync(p => p.CategoryId == request.CategoryId, cancellationToken);
            if (product != null)
                throw new ConflictException("Category cannot be deleted because it still contains products.");

            unitOfWork.Categories.Delete(category);
            await unitOfWork.CompleteAsync(cancellationToken);
            logger.LogInformation("Delete category {CategoryId} operation completed.", request.CategoryId);
            return true;
        }
    }
}
