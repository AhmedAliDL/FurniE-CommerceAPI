using App.Application.Common.Authorization;
using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Reviews.Command.DeleteReview
{
    public class DeleteReviewHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, ILogger<DeleteReviewHandler> logger) : IRequestHandler<DeleteReviewCommand, bool>
    {
        public async Task<bool> Handle(DeleteReviewCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Delete review {ReviewId} operation started.", request.ReviewId);

            var userId = currentUserService.UserId ?? throw new NotFoundException("User not found.");

            var review = await unitOfWork.Reviews.FindAsync(r => r.ReviewId == request.ReviewId, cancellationToken);
            if (review == null)
                return false;

            if (review.UserId != userId && !currentUserService.IsInRole(SupportRoles.Admin))
                throw new ForbiddenException("You can only delete your own reviews.");

            unitOfWork.Reviews.Delete(review);
            await unitOfWork.CompleteAsync(cancellationToken);
            logger.LogInformation("Delete review {ReviewId} operation completed.", request.ReviewId);
            return true;
        }
    }
}
