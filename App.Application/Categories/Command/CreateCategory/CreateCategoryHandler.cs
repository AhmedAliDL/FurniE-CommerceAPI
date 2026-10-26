using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Categories.Command.CreateCategory
{
    public class CreateCategoryHandler(IUnitOfWork unitOfWork, ILogger<CreateCategoryHandler> logger) : IRequestHandler<CreateCategoryCommand, Guid>
    {
        public async Task<Guid> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Create category operation started.");

            var categoryName = request.CategoryName.Trim();
            var existing = await unitOfWork.Categories.FindAsync(
                c => c.CategoryName.ToLower() == categoryName.ToLower(),
                cancellationToken);
            if (existing != null)
                throw new ConflictException($"A category named '{categoryName}' already exists.");

            var category = new Category { CategoryName = categoryName };
            await unitOfWork.Categories.AddAsync(category, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);

            logger.LogInformation("Create category operation completed.");
            return category.CategoryId;
        }
    }
}
