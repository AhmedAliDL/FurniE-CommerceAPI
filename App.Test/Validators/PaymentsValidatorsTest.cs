using App.Application.Payments.Command.CreatePaymentSession;
using App.Application.Payments.Command.RefundPayment;
using App.Application.Payments.Command.VoidPayment;
using App.Application.Payments.Queries.GetPaymentTransactions;
using NUnit.Framework;

namespace App.Test.Validators
{
    [TestFixture]
    [Category("Payments")]
    public class PaymentsValidatorsTest
    {
        [Test]
        public void EnsureThatCreatePaymentSessionCommandIsValidWhenOrderIdProvided()
        {
            var validator = new CreatePaymentSessionValidator();
            var command = new CreatePaymentSessionCommand { OrderId = Guid.NewGuid() };

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatCreatePaymentSessionCommandIsInvalidWhenOrderIdMissing()
        {
            var validator = new CreatePaymentSessionValidator();
            var command = new CreatePaymentSessionCommand { OrderId = Guid.Empty };

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatCreatePaymentSessionCommandIsInvalidWhenIdempotencyKeyTooLong()
        {
            var validator = new CreatePaymentSessionValidator();
            var command = new CreatePaymentSessionCommand { OrderId = Guid.NewGuid(), IdempotencyKey = new string('a', 101) };

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatRefundPaymentCommandIsValidWhenAmountOmitted()
        {
            var validator = new RefundPaymentValidator();
            var command = new RefundPaymentCommand { OrderId = Guid.NewGuid() };

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatRefundPaymentCommandIsInvalidWhenOrderIdMissing()
        {
            var validator = new RefundPaymentValidator();
            var command = new RefundPaymentCommand { OrderId = Guid.Empty };

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatRefundPaymentCommandIsInvalidWhenAmountIsZero()
        {
            var validator = new RefundPaymentValidator();
            var command = new RefundPaymentCommand { OrderId = Guid.NewGuid(), AmountCents = 0 };

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatGetPaymentTransactionsQueryIsValidWithDefaults()
        {
            var validator = new GetPaymentTransactionsValidator();

            var result = validator.Validate(new GetPaymentTransactionsQuery());

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatGetPaymentTransactionsQueryIsInvalidWhenPageIsNotPositive()
        {
            var validator = new GetPaymentTransactionsValidator();

            var result = validator.Validate(new GetPaymentTransactionsQuery { Page = 0 });

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatGetPaymentTransactionsQueryIsInvalidWhenPageSizeIsOutOfRange()
        {
            var validator = new GetPaymentTransactionsValidator();

            var pageSizeZero = validator.Validate(new GetPaymentTransactionsQuery { PageSize = 0 });
            var pageSizeTooLarge = validator.Validate(new GetPaymentTransactionsQuery { PageSize = 101 });

            Assert.That(pageSizeZero.IsValid, Is.False);
            Assert.That(pageSizeTooLarge.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatVoidPaymentCommandIsValidWhenOrderIdProvided()
        {
            var validator = new VoidPaymentValidator();
            var command = new VoidPaymentCommand { OrderId = Guid.NewGuid() };

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatVoidPaymentCommandIsInvalidWhenOrderIdMissing()
        {
            var validator = new VoidPaymentValidator();
            var command = new VoidPaymentCommand { OrderId = Guid.Empty };

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }
    }
}
