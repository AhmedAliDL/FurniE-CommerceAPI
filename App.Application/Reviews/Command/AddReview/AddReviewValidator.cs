using FluentValidation;

namespace App.Application.Reviews.Command.AddReview
{
    public class AddReviewValidator : AbstractValidator<AddReviewCommand>
    {
        public AddReviewValidator()
        {
            RuleFor(r => r.ProductId)
                .NotEmpty()
                .WithMessage("Product is required.");
            RuleFor(r => r.Rating)
                .InclusiveBetween(1, 5)
                .WithMessage("Rating must be between 1 and 5.");
            RuleFor(r => r.Comment)
                .MaximumLength(500)
                .WithMessage("Comment must not exceed 500 characters.");
        }
    }
}
