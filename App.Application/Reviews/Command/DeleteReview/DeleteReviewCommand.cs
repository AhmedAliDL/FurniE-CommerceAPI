using MediatR;

namespace App.Application.Reviews.Command.DeleteReview
{
    public record DeleteReviewCommand(Guid ReviewId) : IRequest<bool>;
}
