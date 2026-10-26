using MediatR;

namespace App.Application.Categories.Command.CreateCategory
{
    public record CreateCategoryCommand : IRequest<Guid>
    {
        public string CategoryName { get; set; } = null!;
    }
}
