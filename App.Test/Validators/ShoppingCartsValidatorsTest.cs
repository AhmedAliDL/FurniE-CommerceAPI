using App.Application.ShoppingCarts.Command.UpdateCartQuantities;
using NUnit.Framework;

namespace App.Test.Validators
{
    [TestFixture]
    [Category("ShoppingCarts")]
    public class ShoppingCartsValidatorsTest
    {
        private UpdateCartQuantitiesValidator _validator = null!;

        [SetUp]
        public void Setup()
        {
            _validator = new UpdateCartQuantitiesValidator();
        }

        [Test]
        public void EnsureThatUpdateCartQuantitiesCommandIsValidWhenItemsAreValid()
        {
            var command = new UpdateCartQuantitiesCommand
            {
                ShoppingCartId = Guid.NewGuid(),
                Items = [new CartItemQuantity { ProductId = Guid.NewGuid(), Quantity = 2 }]
            };

            var result = _validator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatUpdateCartQuantitiesCommandIsInvalidWhenShoppingCartIdMissing()
        {
            var command = new UpdateCartQuantitiesCommand
            {
                Items = [new CartItemQuantity { ProductId = Guid.NewGuid(), Quantity = 2 }]
            };

            var result = _validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatUpdateCartQuantitiesCommandIsInvalidWhenItemQuantityIsNegative()
        {
            var command = new UpdateCartQuantitiesCommand
            {
                ShoppingCartId = Guid.NewGuid(),
                Items = [new CartItemQuantity { ProductId = Guid.NewGuid(), Quantity = -1 }]
            };

            var result = _validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatUpdateCartQuantitiesCommandIsInvalidWhenItemProductMissing()
        {
            var command = new UpdateCartQuantitiesCommand
            {
                ShoppingCartId = Guid.NewGuid(),
                Items = [new CartItemQuantity { Quantity = 2 }]
            };

            var result = _validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }
    }
}
