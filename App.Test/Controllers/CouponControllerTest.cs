using App.Application.Coupons.Command.CreateCoupon;
using App.Application.Coupons.Command.DeleteCoupon;
using App.Application.Coupons.Command.UpdateCoupon;
using App.Application.Coupons.Dto;
using App.Application.Coupons.Queries.CheckCoupon;
using App.Application.Coupons.Queries.GetAllCoupons;
using App.Application.Coupons.Queries.GetCouponForEdit;
using App.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using App.Api.Controllers;

namespace App.Test.Controllers
{
    [TestFixture]
    [Category("Coupons")]
    public class CouponControllerTest
    {
        private Mock<IMediator> _sender = null!;
        private CouponController _controller = null!;

        [SetUp]
        public void Setup()
        {
            _sender = new Mock<IMediator>();
            _controller = new CouponController(_sender.Object);
        }

        [Test]
        public async Task EnsureThatIndexReturnsOkWhenCouponsExist()
        {
            var coupons = new PagedResult<CouponDto> { Page = 1, PageSize = 20, TotalCount = 0 };
            _sender.Setup(s => s.Send(
                    It.IsAny<GetAllCouponsQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(coupons);

            var result = await _controller.GetAllCoupons();

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatCheckCouponReturnsOkWhenCouponIsValid()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<CheckCouponQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResponseResult<decimal>.Success(15m));

            var result = await _controller.CheckCoupon("SAVE15");

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatCheckCouponReturnsOkWhenCouponIsInvalid()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<CheckCouponQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResponseResult<decimal>.Failure("Coupon is expired."));

            var result = await _controller.CheckCoupon("EXPIRED");

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatAddCouponReturnsCreatedWhenCouponIsValid()
        {
            var command = new CreateCouponCommand { CouponCode = "SAVE15", CouponRatio = 15m };
            _sender.Setup(s => s.Send(
                    It.IsAny<CreateCouponCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Guid.NewGuid());

            var result = await _controller.AddCoupon(command);

            Assert.That(result, Is.TypeOf<CreatedAtActionResult>());
        }

        [Test]
        public async Task EnsureThatEditCouponReturnsNoContentWhenCouponUpdated()
        {
            var couponId = Guid.NewGuid();
            var command = new UpdateCouponCommand { CouponId = couponId, CouponRatio = 20m };
            _sender.Setup(s => s.Send(
                    It.IsAny<UpdateCouponCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var result = await _controller.EditCoupon(couponId, command);

            Assert.That(result, Is.TypeOf<NoContentResult>());
        }

        [Test]
        public async Task EnsureThatEditCouponReturnsNotFoundWhenCouponNotExists()
        {
            var couponId = Guid.NewGuid();
            var command = new UpdateCouponCommand { CouponId = couponId, CouponRatio = 20m };
            _sender.Setup(s => s.Send(
                    It.IsAny<UpdateCouponCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var result = await _controller.EditCoupon(couponId, command);

            Assert.That(result, Is.TypeOf<NotFoundResult>());
        }

        [Test]
        public async Task EnsureThatEditCouponReturnsBadRequestWhenRouteIdMismatch()
        {
            var command = new UpdateCouponCommand { CouponId = Guid.NewGuid(), CouponRatio = 20m };

            var result = await _controller.EditCoupon(Guid.NewGuid(), command);

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
            _sender.Verify(s => s.Send(It.IsAny<UpdateCouponCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task EnsureThatDeleteCouponReturnsNoContentWhenCouponDeleted()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<DeleteCouponCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var result = await _controller.DeleteCoupon(Guid.NewGuid());

            Assert.That(result, Is.TypeOf<NoContentResult>());
        }

        [Test]
        public async Task EnsureThatDeleteCouponReturnsNotFoundWhenCouponNotExists()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<DeleteCouponCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var result = await _controller.DeleteCoupon(Guid.NewGuid());

            Assert.That(result, Is.TypeOf<NotFoundResult>());
        }
    }
}
