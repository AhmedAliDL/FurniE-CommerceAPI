using App.Application.Categories.Dto;
using MediatR;

namespace App.Application.Categories.Queries.GetAllCategories
{
    public record GetAllCategoriesQuery : IRequest<List<CategoryDto>>;
}
