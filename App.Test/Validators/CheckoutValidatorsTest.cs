using App.Application.Checkout.Command.PlaceOrder;
using NUnit.Framework;

namespace App.Test.Validators
{
    [TestFixture]
    [Category("Checkout")]
    public class CheckoutValidatorsTest
    {
        private PlaceOrderValidator _validator = null!;

        [SetUp]
        public void Setup()
        {
            _validator = new PlaceOrderValidator();
        }

        [Test]
        public void EnsureThatPlaceOrderCommandIsValidWhenPaymentMethodProvided()
        {
            var command = new PlaceOrderCommand { PaymentMethod = "Paymob", City = "Cairo", Address = "Nasr City" };

            var result = _validator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatPlaceOrderCommandIsInvalidWhenPaymentMethodMissing()
        {
            var command = new PlaceOrderCommand { PaymentMethod = "" };

            var result = _validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatPlaceOrderCommandIsInvalidWhenCardNumberHasWrongLength()
        {
            var command = new PlaceOrderCommand
            {
                PaymentMethod = "CreditCard",
                CardNumber = "123456789012345"
            };

            var result = _validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatPlaceOrderCommandIsValidWhenCardNumberHasSixteenDigits()
        {
            var command = new PlaceOrderCommand
            {
                PaymentMethod = "CreditCard",
                CardNumber = "1234567890123456"
            };

            var result = _validator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatPlaceOrderCommandIsInvalidWhenExpirationMonthOutOfRange()
        {
            var command = new PlaceOrderCommand { PaymentMethod = "CreditCard", ExpirationMonth = 13 };

            var result = _validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatPlaceOrderCommandIsInvalidWhenExpirationYearTooLong()
        {
            var command = new PlaceOrderCommand { PaymentMethod = "CreditCard", ExpirationYear = "2026" };

            var result = _validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatPlaceOrderCommandIsInvalidWhenCVVTooLong()
        {
            var command = new PlaceOrderCommand { PaymentMethod = "CreditCard", CVV = "1234" };

            var result = _validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatPlaceOrderCommandIsInvalidWhenIdempotencyKeyTooLong()
        {
            var command = new PlaceOrderCommand { PaymentMethod = "Paymob", IdempotencyKey = new string('a', 101) };

            var result = _validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }
    }
}
