using App.Application.Wishlists.Command.AddToWishlist;
using App.Application.Wishlists.Command.MoveWishlistItemToCart;
using App.Application.Wishlists.Command.RemoveFromWishlist;
using NUnit.Framework;

namespace App.Test.Validators
{
    [TestFixture]
    [Category("Wishlists")]
    public class WishlistsValidatorsTest
    {
        [Test]
        public void EnsureThatAddToWishlistCommandIsValidWhenProductIdAndPriceProvided()
        {
            var validator = new AddToWishlistValidator();
            var command = new AddToWishlistCommand(Guid.NewGuid(), 150m);

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatAddToWishlistCommandIsInvalidWhenProductIdMissing()
        {
            var validator = new AddToWishlistValidator();
            var command = new AddToWishlistCommand(Guid.Empty, 150m);

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatAddToWishlistCommandIsInvalidWhenPriceIsZero()
        {
            var validator = new AddToWishlistValidator();
            var command = new AddToWishlistCommand(Guid.NewGuid(), 0m);

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatRemoveFromWishlistCommandIsValidWhenProductIdProvided()
        {
            var validator = new RemoveFromWishlistValidator();
            var command = new RemoveFromWishlistCommand(Guid.NewGuid());

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatRemoveFromWishlistCommandIsInvalidWhenProductIdMissing()
        {
            var validator = new RemoveFromWishlistValidator();
            var command = new RemoveFromWishlistCommand(Guid.Empty);

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatMoveWishlistItemToCartCommandIsValidWhenProductIdProvided()
        {
            var validator = new MoveWishlistItemToCartValidator();
            var command = new MoveWishlistItemToCartCommand(Guid.NewGuid());

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatMoveWishlistItemToCartCommandIsInvalidWhenProductIdMissing()
        {
            var validator = new MoveWishlistItemToCartValidator();
            var command = new MoveWishlistItemToCartCommand(Guid.Empty);

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }
    }
}
