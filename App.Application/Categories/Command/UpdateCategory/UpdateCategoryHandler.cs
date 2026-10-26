using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.UnitOfWork;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Categories.Command.UpdateCategory
{
    public class UpdateCategoryHandler(IUnitOfWork unitOfWork, ILogger<UpdateCategoryHandler> logger) : IRequestHandler<UpdateCategoryCommand, bool>
    {
        public async Task<bool> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Update category {CategoryId} operation started.", request.CategoryId);

            var category = await unitOfWork.Categories.FindAsync(c => c.CategoryId == request.CategoryId, cancellationToken);
            if (category == null)
                return false;

            if (request.CategoryName is { } categoryName)
            {
                var trimmedName = categoryName.Trim();
                var duplicate = await unitOfWork.Categories.FindAsync(
                    c => c.CategoryId != request.CategoryId && c.CategoryName.ToLower() == trimmedName.ToLower(),
                    cancellationToken);
                if (duplicate != null)
                    throw new ConflictException($"A category named '{trimmedName}' already exists.");

                category.CategoryName = trimmedName;
            }

            category.UpdatedAt = DateTime.UtcNow;

            unitOfWork.Categories.Update(category);
            await unitOfWork.CompleteAsync(cancellationToken);
            logger.LogInformation("Update category {CategoryId} operation completed.", request.CategoryId);
            return true;
        }
    }
}
