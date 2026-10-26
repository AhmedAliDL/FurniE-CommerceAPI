using MediatR;

namespace App.Application.Categories.Command.UpdateCategory
{
    public record UpdateCategoryCommand : IRequest<bool>
    {
        public Guid CategoryId { get; set; }
        public string? CategoryName { get; set; }
    }
}
