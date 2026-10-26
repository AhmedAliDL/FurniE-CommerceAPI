using FluentValidation;

namespace App.Application.Categories.Command.UpdateCategory
{
    public class UpdateCategoryValidator : AbstractValidator<UpdateCategoryCommand>
    {
        public UpdateCategoryValidator()
        {
            RuleFor(c => c.CategoryId)
                .NotEmpty()
                .WithMessage("Category id is required.");
            RuleFor(c => c.CategoryName)
                .NotEmpty()
                .MaximumLength(50)
                .When(c => c.CategoryName is not null)
                .WithMessage("Category name must not be empty and must not exceed 50 characters.");
        }
    }
}
