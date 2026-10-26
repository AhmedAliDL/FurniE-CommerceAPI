using FluentValidation;

namespace App.Application.Categories.Command.CreateCategory
{
    public class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
    {
        public CreateCategoryValidator()
        {
            RuleFor(c => c.CategoryName)
                .NotEmpty()
                .MaximumLength(50)
                .WithMessage("Category name is required and must not exceed 50 characters.");
        }
    }
}
