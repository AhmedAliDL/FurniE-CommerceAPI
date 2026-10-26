using App.Application.Reviews.Command.AddReview;
using App.Application.Reviews.Command.DeleteReview;
using App.Application.Reviews.Command.UpdateReview;
using App.Application.Reviews.Dto;
using App.Application.Reviews.Queries.GetProductReviews;
using App.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using App.Api.Controllers;

namespace App.Test.Controllers
{
    [TestFixture]
    [Category("Reviews")]
    public class ReviewControllerTest
    {
        private Mock<IMediator> _sender = null!;
        private ReviewController _controller = null!;

        [SetUp]
        public void Setup()
        {
            _sender = new Mock<IMediator>();
            _controller = new ReviewController(_sender.Object);
        }

        [Test]
        public async Task EnsureThatGetProductReviewsReturnsOkWhenReviewsExist()
        {
            var reviews = new PagedResult<ReviewDto> { Page = 1, PageSize = 20, TotalCount = 0 };
            _sender.Setup(s => s.Send(
                    It.IsAny<GetProductReviewsQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(reviews);

            var result = await _controller.GetProductReviews(Guid.NewGuid());

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatAddReviewReturnsCreatedWhenReviewIsValid()
        {
            var command = new AddReviewCommand { ProductId = Guid.NewGuid(), Rating = 4, Comment = "Great product." };
            _sender.Setup(s => s.Send(
                    It.IsAny<AddReviewCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Guid.NewGuid());

            var result = await _controller.AddReview(command);

            Assert.That(result, Is.TypeOf<CreatedAtActionResult>());
        }

        [Test]
        public async Task EnsureThatUpdateReviewReturnsNoContentWhenReviewUpdated()
        {
            var reviewId = Guid.NewGuid();
            var command = new UpdateReviewCommand { ReviewId = reviewId, Rating = 5, Comment = "Updated" };
            _sender.Setup(s => s.Send(
                    It.IsAny<UpdateReviewCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var result = await _controller.UpdateReview(reviewId, command);

            Assert.That(result, Is.TypeOf<NoContentResult>());
        }

        [Test]
        public async Task EnsureThatUpdateReviewReturnsNotFoundWhenReviewDoesNotExist()
        {
            var reviewId = Guid.NewGuid();
            var command = new UpdateReviewCommand { ReviewId = reviewId, Rating = 5, Comment = "Updated" };
            _sender.Setup(s => s.Send(
                    It.IsAny<UpdateReviewCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var result = await _controller.UpdateReview(reviewId, command);

            Assert.That(result, Is.TypeOf<NotFoundResult>());
        }

        [Test]
        public async Task EnsureThatUpdateReviewReturnsBadRequestWhenRouteIdMismatch()
        {
            var command = new UpdateReviewCommand { ReviewId = Guid.NewGuid(), Rating = 5, Comment = "Updated" };

            var result = await _controller.UpdateReview(Guid.NewGuid(), command);

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
            _sender.Verify(s => s.Send(It.IsAny<UpdateReviewCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task EnsureThatDeleteReviewReturnsNoContentWhenReviewDeleted()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<DeleteReviewCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var result = await _controller.DeleteReview(Guid.NewGuid());

            Assert.That(result, Is.TypeOf<NoContentResult>());
        }

        [Test]
        public async Task EnsureThatDeleteReviewReturnsNotFoundWhenReviewDoesNotExist()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<DeleteReviewCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var result = await _controller.DeleteReview(Guid.NewGuid());

            Assert.That(result, Is.TypeOf<NotFoundResult>());
        }
    }
}
