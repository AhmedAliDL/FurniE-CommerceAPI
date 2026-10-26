using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Repos;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Coupons.Command.CreateCoupon;
using App.Application.Coupons.Command.DeleteCoupon;
using App.Application.Coupons.Command.UpdateCoupon;
using App.Application.Coupons.Queries.CheckCoupon;
using App.Application.Coupons.Queries.GetAllCoupons;
using App.Application.Coupons.Queries.GetCouponForEdit;
using App.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace App.Test.Handlers
{
    [TestFixture]
    [Category("Coupons")]
    public class CouponsHandlerTest
    {
        private Mock<IUnitOfWork> _unitOfWork = null!;
        private Mock<IBaseRepository<Coupon>> _coupons = null!;
        private GetAllCouponsHandler _getAllHandler = null!;
        private CheckCouponHandler _checkHandler = null!;
        private GetCouponForEditHandler _getForEditHandler = null!;
        private CreateCouponHandler _createHandler = null!;
        private UpdateCouponHandler _updateHandler = null!;
        private DeleteCouponHandler _deleteHandler = null!;

        [SetUp]
        public void Setup()
        {
            _unitOfWork = new Mock<IUnitOfWork>();
            _coupons = new Mock<IBaseRepository<Coupon>>();
            _unitOfWork.Setup(u => u.Coupons).Returns(_coupons.Object);

            _getAllHandler = new GetAllCouponsHandler(_unitOfWork.Object, new Mock<ILogger<GetAllCouponsHandler>>().Object);
            _checkHandler = new CheckCouponHandler(_unitOfWork.Object, new Mock<ILogger<CheckCouponHandler>>().Object);
            _getForEditHandler = new GetCouponForEditHandler(_unitOfWork.Object, new Mock<ILogger<GetCouponForEditHandler>>().Object);
            _createHandler = new CreateCouponHandler(_unitOfWork.Object, new Mock<ILogger<CreateCouponHandler>>().Object);
            _updateHandler = new UpdateCouponHandler(_unitOfWork.Object, new Mock<ILogger<UpdateCouponHandler>>().Object);
            _deleteHandler = new DeleteCouponHandler(_unitOfWork.Object, new Mock<ILogger<DeleteCouponHandler>>().Object);
        }

        private static Coupon ValidCoupon() => new()
        {
            CouponId = Guid.NewGuid(),
            CouponCode = "SAVE15",
            CouponRatio = 15m,
            ExpireDate = DateTime.Now.AddDays(7)
        };

        [Test]
        public async Task EnsureGetAllCouponsHandlerReturnsDtosWhenCouponsExist()
        {
            _coupons.Setup(r => r.FindPagedAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Coupon, bool>>>(),
                    It.IsAny<System.Linq.Expressions.Expression<Func<Coupon, object>>[]>(),
                    It.IsAny<App.Application.Common.Enums.SortDirection>(),
                    It.IsAny<System.Linq.Expressions.Expression<Func<Coupon, object>>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(((IReadOnlyList<Coupon>)[ValidCoupon()], 1));

            var result = await _getAllHandler.Handle(new GetAllCouponsQuery(), It.IsAny<CancellationToken>());

            Assert.That(result.TotalCount, Is.EqualTo(1));
            Assert.That(result.Items[0].CouponCode, Is.EqualTo("SAVE15"));
            Assert.That(result.Items[0].CouponRatio, Is.EqualTo(15m));
        }

        [Test]
        public async Task EnsureCheckCouponHandlerReturnsFailureWhenCouponCodeIsEmpty()
        {
            var result = await _checkHandler.Handle(new CheckCouponQuery(""), It.IsAny<CancellationToken>());

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Errors, Does.Contain("Invalid coupon"));
        }

        [Test]
        public async Task EnsureCheckCouponHandlerReturnsFailureWhenCouponNotFound()
        {
            _coupons.Setup(r => r.FindAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Coupon, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Coupon?)null);

            var result = await _checkHandler.Handle(new CheckCouponQuery("UNKNOWN"), It.IsAny<CancellationToken>());

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Errors, Does.Contain("Coupon not found"));
        }

        [Test]
        public async Task EnsureCheckCouponHandlerReturnsFailureWhenCouponIsExpired()
        {
            var coupon = ValidCoupon();
            coupon.ExpireDate = DateTime.UtcNow.AddDays(-1);
            _coupons.Setup(r => r.FindAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Coupon, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(coupon);

            var result = await _checkHandler.Handle(new CheckCouponQuery("SAVE15"), It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Errors, Does.Contain("Coupon has expired"));
            });
        }

        [Test]
        public async Task EnsureCheckCouponHandlerReturnsRatioWhenCouponIsValid()
        {
            _coupons.Setup(r => r.FindAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Coupon, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidCoupon());

            var result = await _checkHandler.Handle(new CheckCouponQuery("SAVE15"), It.IsAny<CancellationToken>());

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.EqualTo(15m));
        }

        [Test]
        public async Task EnsureGetCouponForEditHandlerReturnsDtoWhenCouponExists()
        {
            var coupon = ValidCoupon();
            _coupons.Setup(r => r.FindAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Coupon, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(coupon);

            var result = await _getForEditHandler.Handle(new GetCouponForEditQuery(coupon.CouponId), It.IsAny<CancellationToken>());

            Assert.That(result.CouponId, Is.EqualTo(coupon.CouponId));
            Assert.That(result.CouponCode, Is.EqualTo("SAVE15"));
            Assert.That(result.CouponRatio, Is.EqualTo(15m));
            Assert.That(result.PeriodInDays, Is.GreaterThanOrEqualTo(6));
        }

        [Test]
        public void EnsureGetCouponForEditHandlerThrowsNotFoundWhenCouponNotExists()
        {
            _coupons.Setup(r => r.FindAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Coupon, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Coupon?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _getForEditHandler.Handle(new GetCouponForEditQuery(Guid.NewGuid()), It.IsAny<CancellationToken>()));
        }

        [Test]
        public async Task EnsureCreateCouponHandlerSavesCouponWhenCommandIsValid()
        {
            _coupons.Setup(r => r.AddAsync(
                    It.IsAny<Coupon>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _unitOfWork.Setup(u => u.CompleteAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var result = await _createHandler.Handle(
                new CreateCouponCommand { CouponCode = "SAVE20", CouponRatio = 20m, PeriodInDays = 7 },
                It.IsAny<CancellationToken>());

            Assert.That(result, Is.TypeOf<Guid>());
            _coupons.Verify(r => r.AddAsync(It.Is<Coupon>(c => c.CouponCode == "SAVE20" && c.CouponRatio == 20m), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task EnsureUpdateCouponHandlerReturnsTrueWhenCouponExists()
        {
            _coupons.Setup(r => r.FindAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Coupon, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidCoupon());
            _unitOfWork.Setup(u => u.CompleteAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var result = await _updateHandler.Handle(
                new UpdateCouponCommand { CouponId = Guid.NewGuid(), CouponRatio = 25m, PeriodInDays = 3 },
                It.IsAny<CancellationToken>());

            Assert.That(result, Is.True);
            _coupons.Verify(r => r.Update(It.IsAny<Coupon>()), Times.Once);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task EnsureUpdateCouponHandlerReturnsFalseWhenCouponNotExists()
        {
            _coupons.Setup(r => r.FindAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Coupon, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Coupon?)null);

            var result = await _updateHandler.Handle(
                new UpdateCouponCommand { CouponId = Guid.NewGuid(), CouponRatio = 25m },
                It.IsAny<CancellationToken>());

            Assert.That(result, Is.False);
            _coupons.Verify(r => r.Update(It.IsAny<Coupon>()), Times.Never);
        }

        [Test]
        public async Task EnsureDeleteCouponHandlerReturnsTrueWhenCouponExists()
        {
            _coupons.Setup(r => r.FindAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Coupon, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidCoupon());
            _unitOfWork.Setup(u => u.CompleteAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var result = await _deleteHandler.Handle(new DeleteCouponCommand(Guid.NewGuid()), It.IsAny<CancellationToken>());

            Assert.That(result, Is.True);
            _coupons.Verify(r => r.Delete(It.IsAny<Coupon>()), Times.Once);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task EnsureDeleteCouponHandlerReturnsFalseWhenCouponNotExists()
        {
            _coupons.Setup(r => r.FindAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Coupon, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Coupon?)null);

            var result = await _deleteHandler.Handle(new DeleteCouponCommand(Guid.NewGuid()), It.IsAny<CancellationToken>());

            Assert.That(result, Is.False);
            _coupons.Verify(r => r.Delete(It.IsAny<Coupon>()), Times.Never);
        }
    }
}
