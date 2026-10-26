namespace App.Application.Categories.Dto
{
    public sealed class CategoryDto
    {
        public Guid CategoryId { get; init; }
        public string CategoryName { get; init; } = string.Empty;
    }
}
