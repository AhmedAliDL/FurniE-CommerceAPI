using FluentValidation;

namespace App.Application.Users.Queries.GetUsers
{
    public class GetUsersValidator : AbstractValidator<GetUsersQuery>
    {
        public GetUsersValidator()
        {
            RuleFor(q => q.Page)
                .GreaterThanOrEqualTo(1)
                .WithMessage("Page must be at least 1.");
            RuleFor(q => q.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("Page size must be between 1 and 100.");
            RuleFor(q => q.Search)
                .MaximumLength(100)
                .When(q => q.Search is not null)
                .WithMessage("Search must not exceed 100 characters.");
        }
    }
}
