using App.Application.Coupons.Command.CreateCoupon;
using App.Application.Coupons.Command.UpdateCoupon;
using App.Application.Coupons.Queries.GetAllCoupons;
using NUnit.Framework;

namespace App.Test.Validators
{
    [TestFixture]
    [Category("Coupons")]
    public class CouponsValidatorsTest
    {
        [Test]
        public void EnsureThatCreateCouponCommandIsValidWhenFieldsProvided()
        {
            var validator = new CreateCouponValidator();
            var command = new CreateCouponCommand { CouponCode = "SAVE15", CouponRatio = 15m };

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatCreateCouponCommandIsInvalidWhenCodeMissing()
        {
            var validator = new CreateCouponValidator();
            var command = new CreateCouponCommand { CouponCode = "", CouponRatio = 15m };

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatCreateCouponCommandIsInvalidWhenRatioTooLow()
        {
            var validator = new CreateCouponValidator();
            var command = new CreateCouponCommand { CouponCode = "SAVE15", CouponRatio = 0m };

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatCreateCouponCommandIsInvalidWhenRatioTooHigh()
        {
            var validator = new CreateCouponValidator();
            var command = new CreateCouponCommand { CouponCode = "SAVE15", CouponRatio = 101m };

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatUpdateCouponCommandIsValidWhenIdAndRatioProvided()
        {
            var validator = new UpdateCouponValidator();
            var command = new UpdateCouponCommand { CouponId = Guid.NewGuid(), CouponRatio = 20m };

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatUpdateCouponCommandIsValidWhenRatioOmitted()
        {
            var validator = new UpdateCouponValidator();
            var command = new UpdateCouponCommand { CouponId = Guid.NewGuid() };

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatUpdateCouponCommandIsInvalidWhenIdMissing()
        {
            var validator = new UpdateCouponValidator();
            var command = new UpdateCouponCommand { CouponRatio = 20m };

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatUpdateCouponCommandIsInvalidWhenRatioTooHigh()
        {
            var validator = new UpdateCouponValidator();
            var command = new UpdateCouponCommand { CouponId = Guid.NewGuid(), CouponRatio = 150m };

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatGetAllCouponsQueryIsValidWithDefaultValues()
        {
            var validator = new GetAllCouponsValidator();
            var query = new GetAllCouponsQuery();

            var result = validator.Validate(query);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatGetAllCouponsQueryIsInvalidWhenPageIsZero()
        {
            var validator = new GetAllCouponsValidator();
            var query = new GetAllCouponsQuery(Page: 0);

            var result = validator.Validate(query);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatGetAllCouponsQueryIsInvalidWhenPageSizeExceedsMaximum()
        {
            var validator = new GetAllCouponsValidator();
            var query = new GetAllCouponsQuery(PageSize: 101);

            var result = validator.Validate(query);

            Assert.That(result.IsValid, Is.False);
        }
    }
}
