using App.Application.Common.Authorization;
using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Reviews.Command.UpdateReview
{
    public class UpdateReviewHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, ILogger<UpdateReviewHandler> logger) : IRequestHandler<UpdateReviewCommand, bool>
    {
        public async Task<bool> Handle(UpdateReviewCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Update review {ReviewId} operation started.", request.ReviewId);

            var userId = currentUserService.UserId ?? throw new NotFoundException("User not found.");

            var review = await unitOfWork.Reviews.FindAsync(r => r.ReviewId == request.ReviewId, cancellationToken);
            if (review == null)
                return false;

            if (review.UserId != userId && !currentUserService.IsInRole(SupportRoles.Admin))
                throw new ForbiddenException("You can only edit your own reviews.");

            review.Rating = request.Rating;
            review.Comment = request.Comment;
            review.UpdatedAt = DateTime.UtcNow;

            unitOfWork.Reviews.Update(review);
            await unitOfWork.CompleteAsync(cancellationToken);
            logger.LogInformation("Update review {ReviewId} operation completed.", request.ReviewId);
            return true;
        }
    }
}
