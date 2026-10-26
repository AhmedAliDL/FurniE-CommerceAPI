using App.Application.Payments.Command.CreatePaymentSession;
using App.Application.Payments.Command.HandlePaymobCallback;
using App.Application.Payments.Command.RefundPayment;
using App.Application.Payments.Command.VoidPayment;
using App.Application.Payments.Dto;
using App.Application.Payments.Dto.Paymob;
using App.Application.Payments.Queries.GetPaymentTransactions;
using App.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using App.Api.Controllers;

namespace App.Test.Controllers
{
    [TestFixture]
    [Category("Payments")]
    public class PaymentControllerTest
    {
        private Mock<IMediator> _sender = null!;
        private PaymentController _controller = null!;

        [SetUp]
        public void Setup()
        {
            _sender = new Mock<IMediator>();
            _controller = new PaymentController(_sender.Object);
        }

        [Test]
        public async Task EnsureThatCreateSessionReturnsOkWhenSessionIsCreated()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<CreatePaymentSessionCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResponseResult<PaymentSessionDto>.Success(new PaymentSessionDto()));

            var result = await _controller.CreateSession(new CreatePaymentSessionCommand { OrderId = Guid.NewGuid() });

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatCreateSessionReturnsOkWhenOrderNotExists()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<CreatePaymentSessionCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResponseResult<PaymentSessionDto>.Failure("Order was not found."));

            var result = await _controller.CreateSession(new CreatePaymentSessionCommand { OrderId = Guid.NewGuid() });

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatCallbackReturnsOkWhenHmacIsValid()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<HandlePaymobCallbackCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var result = await _controller.Callback(new PaymobCallbackDto(), "valid-hmac");

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatCallbackReturnsOkWhenHmacIsMissing()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<HandlePaymobCallbackCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var result = await _controller.Callback(new PaymobCallbackDto(), null);

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatGetTransactionsReturnsOkWhenTransactionsExist()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<GetPaymentTransactionsQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedResult<PaymentTransactionResponseDto> { Items = [], Page = 1, PageSize = 10, TotalCount = 0 });

            var result = await _controller.GetTransactions(new GetPaymentTransactionsQuery());

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatGetAllTransactionsReturnsOkAndSendsAnUnfilteredQuery()
        {
            _sender.Setup(s => s.Send(
                    It.Is<GetPaymentTransactionsQuery>(q => q.OrderId == null && q.Page == 2 && q.PageSize == 50),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedResult<PaymentTransactionResponseDto> { Items = [], Page = 2, PageSize = 50, TotalCount = 0 });

            var result = await _controller.GetAllTransactions(2, 50);

            Assert.That(result, Is.TypeOf<OkObjectResult>());
            _sender.Verify(s => s.Send(
                It.Is<GetPaymentTransactionsQuery>(q => q.OrderId == null && q.Page == 2 && q.PageSize == 50),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task EnsureThatRefundReturnsOkWhenRefundSucceeds()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<RefundPaymentCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResponseResult<PaymentOperationResultDto>.Success(new PaymentOperationResultDto()));

            var result = await _controller.Refund(new RefundPaymentCommand { OrderId = Guid.NewGuid() });

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatRefundReturnsOkWhenRefundFails()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<RefundPaymentCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResponseResult<PaymentOperationResultDto>.Failure("Refunds are only allowed within 14 days after delivery."));

            var result = await _controller.Refund(new RefundPaymentCommand { OrderId = Guid.NewGuid() });

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatVoidReturnsOkWhenVoidSucceeds()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<VoidPaymentCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResponseResult<PaymentOperationResultDto>.Success(new PaymentOperationResultDto()));

            var result = await _controller.Void(new VoidPaymentCommand { OrderId = Guid.NewGuid() });

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatVoidReturnsOkWhenVoidFails()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<VoidPaymentCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResponseResult<PaymentOperationResultDto>.Failure("Voids are only allowed within 24 hours of payment."));

            var result = await _controller.Void(new VoidPaymentCommand { OrderId = Guid.NewGuid() });

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }
    }
}
