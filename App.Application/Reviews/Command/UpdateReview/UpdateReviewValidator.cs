using FluentValidation;

namespace App.Application.Reviews.Command.UpdateReview
{
    public class UpdateReviewValidator : AbstractValidator<UpdateReviewCommand>
    {
        public UpdateReviewValidator()
        {
            RuleFor(r => r.ReviewId)
                .NotEmpty()
                .WithMessage("Review id is required.");
            RuleFor(r => r.Rating)
                .InclusiveBetween(1, 5)
                .WithMessage("Rating must be between 1 and 5.");
            RuleFor(r => r.Comment)
                .MaximumLength(500)
                .WithMessage("Comment must not exceed 500 characters.");
        }
    }
}
