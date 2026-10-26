using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Reviews.Command.AddReview
{
    public class AddReviewHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, ILogger<AddReviewHandler> logger) : IRequestHandler<AddReviewCommand, Guid>
    {
        public async Task<Guid> Handle(AddReviewCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Add review operation started.");
            Guid userId = currentUserService.UserId ?? throw new NotFoundException("User not found.");

            var product = await unitOfWork.Products.FindAsync(p => p.ProductId == request.ProductId, cancellationToken)
                ?? throw new NotFoundException($"Product with id {request.ProductId} was not found.");

            var existingReview = await unitOfWork.Reviews.FindAsync(
                r => r.ProductId == request.ProductId && r.UserId == userId,
                cancellationToken);
            if (existingReview != null)
                throw new ConflictException("You have already reviewed this product.");

            var review = new Review
            {
                ProductId = request.ProductId,
                Rating = request.Rating,
                Comment = request.Comment,
                UserId = userId
            };
            await unitOfWork.Reviews.AddAsync(review, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);
            logger.LogInformation("Add review operation completed.");
            return review.ReviewId;
        }
    }
}
