using App.Application.Reviews.Command.AddReview;
using App.Application.Reviews.Command.UpdateReview;
using App.Application.Reviews.Queries.GetProductReviews;
using NUnit.Framework;

namespace App.Test.Validators
{
    [TestFixture]
    [Category("Reviews")]
    public class ReviewsValidatorsTest
    {
        private AddReviewValidator _validator = null!;

        [SetUp]
        public void Setup()
        {
            _validator = new AddReviewValidator();
        }

        [Test]
        public void EnsureThatAddReviewCommandIsValidWhenAllFieldsProvided()
        {
            var command = new AddReviewCommand { ProductId = Guid.NewGuid(), Rating = 5, Comment = "Great product" };

            var result = _validator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatAddReviewCommandIsInvalidWhenProductMissing()
        {
            var command = new AddReviewCommand { Rating = 5, Comment = "Great product" };

            var result = _validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatAddReviewCommandIsInvalidWhenRatingBelowRange()
        {
            var command = new AddReviewCommand { ProductId = Guid.NewGuid(), Rating = 0, Comment = "Bad product" };

            var result = _validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatAddReviewCommandIsInvalidWhenRatingAboveRange()
        {
            var command = new AddReviewCommand { ProductId = Guid.NewGuid(), Rating = 6, Comment = "Great product" };

            var result = _validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatAddReviewCommandIsInvalidWhenCommentTooLong()
        {
            var command = new AddReviewCommand { ProductId = Guid.NewGuid(), Rating = 5, Comment = new string('a', 501) };

            var result = _validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatUpdateReviewCommandIsValidWhenAllFieldsProvided()
        {
            var validator = new UpdateReviewValidator();
            var command = new UpdateReviewCommand { ReviewId = Guid.NewGuid(), Rating = 4, Comment = "Updated" };

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatUpdateReviewCommandIsInvalidWhenReviewIdMissing()
        {
            var validator = new UpdateReviewValidator();
            var command = new UpdateReviewCommand { Rating = 4, Comment = "Updated" };

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatUpdateReviewCommandIsInvalidWhenRatingOutOfRange()
        {
            var validator = new UpdateReviewValidator();
            var command = new UpdateReviewCommand { ReviewId = Guid.NewGuid(), Rating = 6, Comment = "Updated" };

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatUpdateReviewCommandIsInvalidWhenCommentTooLong()
        {
            var validator = new UpdateReviewValidator();
            var command = new UpdateReviewCommand { ReviewId = Guid.NewGuid(), Rating = 4, Comment = new string('a', 501) };

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatGetProductReviewsQueryIsValidWithDefaultValues()
        {
            var validator = new GetProductReviewsValidator();
            var query = new GetProductReviewsQuery(Guid.NewGuid());

            var result = validator.Validate(query);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatGetProductReviewsQueryIsInvalidWhenProductIdMissing()
        {
            var validator = new GetProductReviewsValidator();
            var query = new GetProductReviewsQuery(Guid.Empty);

            var result = validator.Validate(query);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatGetProductReviewsQueryIsInvalidWhenPageSizeExceedsMaximum()
        {
            var validator = new GetProductReviewsValidator();
            var query = new GetProductReviewsQuery(Guid.NewGuid(), PageSize: 101);

            var result = validator.Validate(query);

            Assert.That(result.IsValid, Is.False);
        }
    }
}
