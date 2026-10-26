using MediatR;

namespace App.Application.Categories.Command.DeleteCategory
{
    public record DeleteCategoryCommand(Guid CategoryId) : IRequest<bool>;
}
