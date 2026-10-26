using App.Application.Categories.Dto;
using App.Application.Common.Interfaces.UnitOfWork;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Categories.Queries.GetAllCategories
{
    public class GetAllCategoriesHandler(IUnitOfWork unitOfWork, ILogger<GetAllCategoriesHandler> logger) : IRequestHandler<GetAllCategoriesQuery, List<CategoryDto>>
    {
        public async Task<List<CategoryDto>> Handle(GetAllCategoriesQuery request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Get all categories operation started.");

            var categories = await unitOfWork.Categories.GetAllAsync(cancellationToken);
            var categoryDtos = categories.Select(c => new CategoryDto
            {
                CategoryId = c.CategoryId,
                CategoryName = c.CategoryName
            }).ToList();

            logger.LogInformation("Get all categories operation completed. {Count} categories returned.", categoryDtos.Count);
            return categoryDtos;
        }
    }
}
