using App.Application.Common.Authorization;
using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Repos;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Reviews.Command.AddReview;
using App.Application.Reviews.Command.DeleteReview;
using App.Application.Reviews.Command.UpdateReview;
using App.Application.Reviews.Queries.GetProductReviews;
using App.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Linq.Expressions;

namespace App.Test.Handlers
{
    [TestFixture]
    [Category("Reviews")]
    public class ReviewsHandlerTest
    {
        private Mock<IUnitOfWork> _unitOfWork = null!;
        private Mock<IBaseRepository<Review>> _reviews = null!;
        private Mock<IBaseRepository<Product>> _products = null!;
        private Mock<ICurrentUserService> _currentUserService = null!;
        private AddReviewHandler _addHandler = null!;
        private UpdateReviewHandler _updateHandler = null!;
        private DeleteReviewHandler _deleteHandler = null!;
        private GetProductReviewsHandler _getProductReviewsHandler = null!;

        [SetUp]
        public void Setup()
        {
            _unitOfWork = new Mock<IUnitOfWork>();
            _reviews = new Mock<IBaseRepository<Review>>();
            _products = new Mock<IBaseRepository<Product>>();
            _currentUserService = new Mock<ICurrentUserService>();
            _unitOfWork.Setup(u => u.Reviews).Returns(_reviews.Object);
            _unitOfWork.Setup(u => u.Products).Returns(_products.Object);

            _addHandler = new AddReviewHandler(_unitOfWork.Object, _currentUserService.Object, new Mock<ILogger<AddReviewHandler>>().Object);
            _updateHandler = new UpdateReviewHandler(_unitOfWork.Object, _currentUserService.Object, new Mock<ILogger<UpdateReviewHandler>>().Object);
            _deleteHandler = new DeleteReviewHandler(_unitOfWork.Object, _currentUserService.Object, new Mock<ILogger<DeleteReviewHandler>>().Object);
            _getProductReviewsHandler = new GetProductReviewsHandler(_unitOfWork.Object, new Mock<ILogger<GetProductReviewsHandler>>().Object);
        }

        [Test]
        public async Task EnsureAddReviewHandlerSavesReviewWhenUserIsAuthenticated()
        {
            var userId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            _currentUserService.Setup(s => s.UserId).Returns(userId);
            _products.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Product, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Product { ProductId = productId });
            _reviews.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Review, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Review?)null);
            _reviews.Setup(r => r.AddAsync(
                    It.IsAny<Review>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _unitOfWork.Setup(u => u.CompleteAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var result = await _addHandler.Handle(
                new AddReviewCommand { ProductId = productId, Rating = 5, Comment = "Great product" },
                It.IsAny<CancellationToken>());

            Assert.That(result, Is.TypeOf<Guid>());
            _reviews.Verify(r => r.AddAsync(It.Is<Review>(rv => rv.UserId == userId && rv.Rating == 5), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void EnsureAddReviewHandlerThrowsNotFoundWhenUserNotResolved()
        {
            _currentUserService.Setup(s => s.UserId).Returns((Guid?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _addHandler.Handle(
                    new AddReviewCommand { ProductId = Guid.NewGuid(), Rating = 5, Comment = "Great product" },
                    It.IsAny<CancellationToken>()));
        }

        [Test]
        public void EnsureAddReviewHandlerThrowsNotFoundWhenProductNotExists()
        {
            var userId = Guid.NewGuid();
            _currentUserService.Setup(s => s.UserId).Returns(userId);
            _products.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Product, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Product?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _addHandler.Handle(
                    new AddReviewCommand { ProductId = Guid.NewGuid(), Rating = 5, Comment = "Great product" },
                    It.IsAny<CancellationToken>()));
            _reviews.Verify(r => r.AddAsync(It.IsAny<Review>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task EnsureAddReviewHandlerThrowsConflictWhenUserAlreadyReviewedProduct()
        {
            var userId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            _currentUserService.Setup(s => s.UserId).Returns(userId);
            _products.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Product, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Product { ProductId = productId });
            _reviews.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Review, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Review { ProductId = productId, UserId = userId, Rating = 4 });

            var exception = await Assert.ThrowsAsync<ConflictException>(async () =>
                await _addHandler.Handle(
                    new AddReviewCommand { ProductId = productId, Rating = 5, Comment = "Great product" },
                    It.IsAny<CancellationToken>()));

            Assert.That(exception!.Message, Does.Contain("already reviewed"));
            _reviews.Verify(r => r.AddAsync(It.IsAny<Review>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task EnsureUpdateReviewHandlerUpdatesOwnReview()
        {
            var userId = Guid.NewGuid();
            var review = new Review { ReviewId = Guid.NewGuid(), UserId = userId, Rating = 3, Comment = "Okay" };
            _currentUserService.Setup(s => s.UserId).Returns(userId);
            _reviews.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Review, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(review);
            _unitOfWork.Setup(u => u.CompleteAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var result = await _updateHandler.Handle(
                new UpdateReviewCommand { ReviewId = review.ReviewId, Rating = 5, Comment = "Excellent" },
                It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(review.Rating, Is.EqualTo(5));
                Assert.That(review.Comment, Is.EqualTo("Excellent"));
                Assert.That(review.UpdatedAt, Is.Not.Null);
            });
            _reviews.Verify(r => r.Update(review), Times.Once);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task EnsureUpdateReviewHandlerAllowsAdminToUpdateAnotherUsersReview()
        {
            var review = new Review { ReviewId = Guid.NewGuid(), UserId = Guid.NewGuid(), Rating = 2, Comment = "Poor" };
            _currentUserService.Setup(s => s.UserId).Returns(Guid.NewGuid());
            _currentUserService.Setup(s => s.IsInRole(SupportRoles.Admin)).Returns(true);
            _reviews.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Review, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(review);
            _unitOfWork.Setup(u => u.CompleteAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var result = await _updateHandler.Handle(
                new UpdateReviewCommand { ReviewId = review.ReviewId, Rating = 1, Comment = "Moderated" },
                It.IsAny<CancellationToken>());

            Assert.That(result, Is.True);
            Assert.That(review.Comment, Is.EqualTo("Moderated"));
        }

        [Test]
        public void EnsureUpdateReviewHandlerThrowsForbiddenWhenNotOwnerAndNotAdmin()
        {
            var review = new Review { ReviewId = Guid.NewGuid(), UserId = Guid.NewGuid(), Rating = 2, Comment = "Poor" };
            _currentUserService.Setup(s => s.UserId).Returns(Guid.NewGuid());
            _currentUserService.Setup(s => s.IsInRole(SupportRoles.Admin)).Returns(false);
            _reviews.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Review, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(review);

            Assert.ThrowsAsync<ForbiddenException>(async () =>
                await _updateHandler.Handle(
                    new UpdateReviewCommand { ReviewId = review.ReviewId, Rating = 5, Comment = "Hijacked" },
                    It.IsAny<CancellationToken>()));
            _reviews.Verify(r => r.Update(It.IsAny<Review>()), Times.Never);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task EnsureUpdateReviewHandlerReturnsFalseWhenReviewNotExists()
        {
            _currentUserService.Setup(s => s.UserId).Returns(Guid.NewGuid());
            _reviews.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Review, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Review?)null);

            var result = await _updateHandler.Handle(
                new UpdateReviewCommand { ReviewId = Guid.NewGuid(), Rating = 5, Comment = "Excellent" },
                It.IsAny<CancellationToken>());

            Assert.That(result, Is.False);
            _reviews.Verify(r => r.Update(It.IsAny<Review>()), Times.Never);
        }

        [Test]
        public async Task EnsureDeleteReviewHandlerDeletesOwnReview()
        {
            var userId = Guid.NewGuid();
            var review = new Review { ReviewId = Guid.NewGuid(), UserId = userId, Rating = 4 };
            _currentUserService.Setup(s => s.UserId).Returns(userId);
            _reviews.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Review, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(review);
            _unitOfWork.Setup(u => u.CompleteAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var result = await _deleteHandler.Handle(
                new DeleteReviewCommand(review.ReviewId),
                It.IsAny<CancellationToken>());

            Assert.That(result, Is.True);
            _reviews.Verify(r => r.Delete(review), Times.Once);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task EnsureDeleteReviewHandlerAllowsAdminToDeleteAnotherUsersReview()
        {
            var review = new Review { ReviewId = Guid.NewGuid(), UserId = Guid.NewGuid(), Rating = 1 };
            _currentUserService.Setup(s => s.UserId).Returns(Guid.NewGuid());
            _currentUserService.Setup(s => s.IsInRole(SupportRoles.Admin)).Returns(true);
            _reviews.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Review, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(review);
            _unitOfWork.Setup(u => u.CompleteAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var result = await _deleteHandler.Handle(
                new DeleteReviewCommand(review.ReviewId),
                It.IsAny<CancellationToken>());

            Assert.That(result, Is.True);
            _reviews.Verify(r => r.Delete(review), Times.Once);
        }

        [Test]
        public void EnsureDeleteReviewHandlerThrowsForbiddenWhenNotOwnerAndNotAdmin()
        {
            var review = new Review { ReviewId = Guid.NewGuid(), UserId = Guid.NewGuid(), Rating = 1 };
            _currentUserService.Setup(s => s.UserId).Returns(Guid.NewGuid());
            _currentUserService.Setup(s => s.IsInRole(SupportRoles.Admin)).Returns(false);
            _reviews.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Review, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(review);

            Assert.ThrowsAsync<ForbiddenException>(async () =>
                await _deleteHandler.Handle(
                    new DeleteReviewCommand(review.ReviewId),
                    It.IsAny<CancellationToken>()));
            _reviews.Verify(r => r.Delete(It.IsAny<Review>()), Times.Never);
        }

        [Test]
        public async Task EnsureDeleteReviewHandlerReturnsFalseWhenReviewNotExists()
        {
            _currentUserService.Setup(s => s.UserId).Returns(Guid.NewGuid());
            _reviews.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Review, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Review?)null);

            var result = await _deleteHandler.Handle(
                new DeleteReviewCommand(Guid.NewGuid()),
                It.IsAny<CancellationToken>());

            Assert.That(result, Is.False);
            _reviews.Verify(r => r.Delete(It.IsAny<Review>()), Times.Never);
        }

        [Test]
        public async Task EnsureGetProductReviewsHandlerReturnsDtosWhenReviewsExist()
        {
            _reviews.Setup(r => r.FindPagedAsync(
                    It.IsAny<Expression<Func<Review, bool>>>(),
                    It.IsAny<Expression<Func<Review, object>>[]>(),
                    It.IsAny<App.Application.Common.Enums.SortDirection>(),
                    It.IsAny<Expression<Func<Review, object>>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(((IReadOnlyList<Review>)
                [
                    new Review
                    {
                        ReviewId = Guid.NewGuid(),
                        ProductId = Guid.NewGuid(),
                        Rating = 4,
                        Comment = "Solid chair",
                        Customer = new AppUser { FName = "Ahmed", LName = "Ali" }
                    }
                ], 1));

            var result = await _getProductReviewsHandler.Handle(new GetProductReviewsQuery(Guid.NewGuid()), It.IsAny<CancellationToken>());

            Assert.That(result.TotalCount, Is.EqualTo(1));
            Assert.That(result.Items[0].Rating, Is.EqualTo(4));
            Assert.That(result.Items[0].CustomerName, Is.EqualTo("Ahmed Ali"));
            Assert.That(result.Items[0].Comment, Is.EqualTo("Solid chair"));
        }

        [Test]
        public async Task EnsureGetProductReviewsHandlerReturnsEmptyListWhenNoReviewsExist()
        {
            _reviews.Setup(r => r.FindPagedAsync(
                    It.IsAny<Expression<Func<Review, bool>>>(),
                    It.IsAny<Expression<Func<Review, object>>[]>(),
                    It.IsAny<App.Application.Common.Enums.SortDirection>(),
                    It.IsAny<Expression<Func<Review, object>>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(((IReadOnlyList<Review>)[], 0));

            var result = await _getProductReviewsHandler.Handle(new GetProductReviewsQuery(Guid.NewGuid()), It.IsAny<CancellationToken>());

            Assert.That(result.Items, Is.Empty);
        }
    }
}
