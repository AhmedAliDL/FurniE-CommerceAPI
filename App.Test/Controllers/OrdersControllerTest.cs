using App.Application.Orders.Command.AcceptReturnRequest;
using App.Application.Orders.Command.ApproveCancellationRequest;
using App.Application.Orders.Command.CancelOrder;
using App.Application.Orders.Command.DeleteOrder;
using App.Application.Orders.Command.DeleteOrderItem;
using App.Application.Orders.Command.ProcessReturnRefund;
using App.Application.Orders.Command.ReceiveReturnRequest;
using App.Application.Orders.Command.RejectCancellationRequest;
using App.Application.Orders.Command.RejectReturnRequest;
using App.Application.Orders.Command.RequestOrderCancellation;
using App.Application.Orders.Command.RequestOrderReturn;
using App.Application.Orders.Command.UpdateOrderStatus;
using App.Application.Orders.Dto;
using App.Application.Orders.Queries.GetCancellationRequests;
using App.Application.Orders.Queries.GetOrderDetails;
using App.Application.Orders.Queries.GetReturnRequests;
using App.Application.Orders.Queries.GetUserOrders;
using App.Application.Responses;
using App.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using App.Api.Controllers;

namespace App.Test.Controllers
{
    [TestFixture]
    [Category("Orders")]
    public class OrdersControllerTest
    {
        private Mock<IMediator> _sender = null!;
        private OrdersController _controller = null!;

        [SetUp]
        public void Setup()
        {
            _sender = new Mock<IMediator>();
            _controller = new OrdersController(_sender.Object);
        }

        [Test]
        public async Task EnsureThatGetOrdersReturnsOkWhenOrdersExist()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<GetUserOrdersQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedResult<OrderShippingDto> { Page = 1, PageSize = 20, TotalCount = 0 });

            var result = await _controller.GetOrders();

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatOrderDetailsReturnsOkWhenOrderExists()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<GetOrderDetailsQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<OrderDetailsDto>());

            var result = await _controller.OrderDetails(Guid.NewGuid());

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatUpdateOrderStatusReturnsNoContentWhenStatusUpdated()
        {
            var orderId = Guid.NewGuid();
            var command = new UpdateOrderStatusCommand(orderId, OrderStatus.Confirmed);
            _sender.Setup(s => s.Send(
                    It.IsAny<UpdateOrderStatusCommand>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.UpdateOrderStatus(orderId, command);

            Assert.That(result, Is.TypeOf<NoContentResult>());
        }

        [Test]
        public async Task EnsureThatUpdateOrderStatusReturnsBadRequestWhenRouteIdMismatch()
        {
            var command = new UpdateOrderStatusCommand(Guid.NewGuid(), OrderStatus.Confirmed);

            var result = await _controller.UpdateOrderStatus(Guid.NewGuid(), command);

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
            _sender.Verify(s => s.Send(It.IsAny<UpdateOrderStatusCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task EnsureThatCancelOrderReturnsNoContentWhenOrderCancelled()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<CancelOrderCommand>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.CancelOrder(Guid.NewGuid());

            Assert.That(result, Is.TypeOf<NoContentResult>());
        }

        [Test]
        public async Task EnsureThatRequestOrderCancellationReturnsOkWhenRequestCreated()
        {
            var orderId = Guid.NewGuid();
            var command = new RequestOrderCancellationCommand(orderId, "Found a better price");
            _sender.Setup(s => s.Send(
                    It.IsAny<RequestOrderCancellationCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResponseResult<Guid>.Success(Guid.NewGuid()));

            var result = await _controller.RequestOrderCancellation(orderId, command);

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatRequestOrderCancellationReturnsBadRequestWhenRequestRejected()
        {
            var orderId = Guid.NewGuid();
            var command = new RequestOrderCancellationCommand(orderId, "Found a better price");
            _sender.Setup(s => s.Send(
                    It.IsAny<RequestOrderCancellationCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResponseResult<Guid>.Failure("Order is already shipped."));

            var result = await _controller.RequestOrderCancellation(orderId, command);

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task EnsureThatRequestOrderCancellationReturnsBadRequestWhenRouteIdMismatch()
        {
            var command = new RequestOrderCancellationCommand(Guid.NewGuid(), "Found a better price");

            var result = await _controller.RequestOrderCancellation(Guid.NewGuid(), command);

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
            _sender.Verify(s => s.Send(It.IsAny<RequestOrderCancellationCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task EnsureThatRequestOrderReturnReturnsOkWhenRequestCreated()
        {
            var orderId = Guid.NewGuid();
            var command = new RequestOrderReturnCommand(orderId, "MerchantDefect", "Broken leg", []);
            _sender.Setup(s => s.Send(
                    It.IsAny<RequestOrderReturnCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResponseResult<Guid>.Success(Guid.NewGuid()));

            var result = await _controller.RequestOrderReturn(orderId, command);

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatRequestOrderReturnReturnsBadRequestWhenRequestRejected()
        {
            var orderId = Guid.NewGuid();
            var command = new RequestOrderReturnCommand(orderId, "MerchantDefect", "Broken leg", []);
            _sender.Setup(s => s.Send(
                    It.IsAny<RequestOrderReturnCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResponseResult<Guid>.Failure("Returns are only allowed within 14 days of delivery."));

            var result = await _controller.RequestOrderReturn(orderId, command);

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task EnsureThatRequestOrderReturnReturnsBadRequestWhenRouteIdMismatch()
        {
            var command = new RequestOrderReturnCommand(Guid.NewGuid(), "MerchantDefect", "Broken leg", []);

            var result = await _controller.RequestOrderReturn(Guid.NewGuid(), command);

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
            _sender.Verify(s => s.Send(It.IsAny<RequestOrderReturnCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task EnsureThatGetCancellationRequestsReturnsOkWhenRequestsExist()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<GetCancellationRequestsQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResponseResult<PagedResult<CancellationRequestListDto>>.Success(
                    new PagedResult<CancellationRequestListDto> { Page = 1, PageSize = 20, TotalCount = 0 }));

            var result = await _controller.GetCancellationRequests(null);

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatGetReturnRequestsReturnsOkWhenRequestsExist()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<GetReturnRequestsQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResponseResult<PagedResult<ReturnRequestListDto>>.Success(
                    new PagedResult<ReturnRequestListDto> { Page = 1, PageSize = 20, TotalCount = 0 }));

            var result = await _controller.GetReturnRequests(null);

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatApproveCancellationRequestReturnsNoContentWhenApproved()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<ApproveCancellationRequestCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResponseResult.Success());

            var result = await _controller.ApproveCancellationRequest(Guid.NewGuid());

            Assert.That(result, Is.TypeOf<NoContentResult>());
        }

        [Test]
        public async Task EnsureThatApproveCancellationRequestReturnsBadRequestWhenApprovalFails()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<ApproveCancellationRequestCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResponseResult.Failure("Cancellation request is already processed."));

            var result = await _controller.ApproveCancellationRequest(Guid.NewGuid());

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task EnsureThatRejectCancellationRequestReturnsNoContentWhenRejected()
        {
            var requestId = Guid.NewGuid();
            var command = new RejectCancellationRequestCommand(requestId, "Order is already packed");
            _sender.Setup(s => s.Send(
                    It.IsAny<RejectCancellationRequestCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResponseResult.Success());

            var result = await _controller.RejectCancellationRequest(requestId, command);

            Assert.That(result, Is.TypeOf<NoContentResult>());
        }

        [Test]
        public async Task EnsureThatRejectCancellationRequestReturnsBadRequestWhenRejectionFails()
        {
            var requestId = Guid.NewGuid();
            var command = new RejectCancellationRequestCommand(requestId, "Order is already packed");
            _sender.Setup(s => s.Send(
                    It.IsAny<RejectCancellationRequestCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResponseResult.Failure("Cancellation request is already processed."));

            var result = await _controller.RejectCancellationRequest(requestId, command);

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task EnsureThatRejectCancellationRequestReturnsBadRequestWhenRouteIdMismatch()
        {
            var command = new RejectCancellationRequestCommand(Guid.NewGuid(), "Order is already packed");

            var result = await _controller.RejectCancellationRequest(Guid.NewGuid(), command);

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
            _sender.Verify(s => s.Send(It.IsAny<RejectCancellationRequestCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task EnsureThatAcceptReturnRequestReturnsNoContentWhenAccepted()
        {
            var requestId = Guid.NewGuid();
            var command = new AcceptReturnRequestCommand(requestId, "Aramex", "01001234567");
            _sender.Setup(s => s.Send(
                    It.IsAny<AcceptReturnRequestCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResponseResult.Success());

            var result = await _controller.AcceptReturnRequest(requestId, command);

            Assert.That(result, Is.TypeOf<NoContentResult>());
        }

        [Test]
        public async Task EnsureThatAcceptReturnRequestReturnsBadRequestWhenAcceptanceFails()
        {
            var requestId = Guid.NewGuid();
            var command = new AcceptReturnRequestCommand(requestId, "Aramex", "01001234567");
            _sender.Setup(s => s.Send(
                    It.IsAny<AcceptReturnRequestCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResponseResult.Failure("Return request is already processed."));

            var result = await _controller.AcceptReturnRequest(requestId, command);

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task EnsureThatAcceptReturnRequestReturnsBadRequestWhenRouteIdMismatch()
        {
            var command = new AcceptReturnRequestCommand(Guid.NewGuid(), "Aramex", "01001234567");

            var result = await _controller.AcceptReturnRequest(Guid.NewGuid(), command);

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
            _sender.Verify(s => s.Send(It.IsAny<AcceptReturnRequestCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task EnsureThatReceiveReturnRequestReturnsNoContentWhenItemsReceived()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<ReceiveReturnRequestCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResponseResult.Success());

            var result = await _controller.ReceiveReturnRequest(Guid.NewGuid());

            Assert.That(result, Is.TypeOf<NoContentResult>());
        }

        [Test]
        public async Task EnsureThatReceiveReturnRequestReturnsBadRequestWhenReceivingFails()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<ReceiveReturnRequestCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResponseResult.Failure("Return request is not accepted yet."));

            var result = await _controller.ReceiveReturnRequest(Guid.NewGuid());

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task EnsureThatRejectReturnRequestReturnsNoContentWhenRejected()
        {
            var requestId = Guid.NewGuid();
            var command = new RejectReturnRequestCommand(requestId, "Items were used");
            _sender.Setup(s => s.Send(
                    It.IsAny<RejectReturnRequestCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResponseResult.Success());

            var result = await _controller.RejectReturnRequest(requestId, command);

            Assert.That(result, Is.TypeOf<NoContentResult>());
        }

        [Test]
        public async Task EnsureThatRejectReturnRequestReturnsBadRequestWhenRejectionFails()
        {
            var requestId = Guid.NewGuid();
            var command = new RejectReturnRequestCommand(requestId, "Items were used");
            _sender.Setup(s => s.Send(
                    It.IsAny<RejectReturnRequestCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResponseResult.Failure("Return request is already processed."));

            var result = await _controller.RejectReturnRequest(requestId, command);

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task EnsureThatRejectReturnRequestReturnsBadRequestWhenRouteIdMismatch()
        {
            var command = new RejectReturnRequestCommand(Guid.NewGuid(), "Items were used");

            var result = await _controller.RejectReturnRequest(Guid.NewGuid(), command);

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
            _sender.Verify(s => s.Send(It.IsAny<RejectReturnRequestCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task EnsureThatProcessReturnRefundReturnsNoContentWhenRefundProcessed()
        {
            var requestId = Guid.NewGuid();
            var command = new ProcessReturnRefundCommand(requestId, AmountCents: 5000);
            _sender.Setup(s => s.Send(
                    It.IsAny<ProcessReturnRefundCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResponseResult.Success());

            var result = await _controller.ProcessReturnRefund(requestId, command);

            Assert.That(result, Is.TypeOf<NoContentResult>());
        }

        [Test]
        public async Task EnsureThatProcessReturnRefundReturnsBadRequestWhenRefundFails()
        {
            var requestId = Guid.NewGuid();
            var command = new ProcessReturnRefundCommand(requestId, AmountCents: 5000);
            _sender.Setup(s => s.Send(
                    It.IsAny<ProcessReturnRefundCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResponseResult.Failure("Return request has not been received yet."));

            var result = await _controller.ProcessReturnRefund(requestId, command);

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task EnsureThatProcessReturnRefundReturnsBadRequestWhenRouteIdMismatch()
        {
            var command = new ProcessReturnRefundCommand(Guid.NewGuid(), AmountCents: 5000);

            var result = await _controller.ProcessReturnRefund(Guid.NewGuid(), command);

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
            _sender.Verify(s => s.Send(It.IsAny<ProcessReturnRefundCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task EnsureThatDeleteFullOrderReturnsNoContentWhenOrderDeleted()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<DeleteOrderCommand>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.DeleteFullOrder(Guid.NewGuid());

            Assert.That(result, Is.TypeOf<NoContentResult>());
        }

        [Test]
        public async Task EnsureThatDeleteSpecificItemFromOrderReturnsNoContentWhenItemDeleted()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<DeleteOrderItemCommand>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.DeleteSpecificItemFromOrder(Guid.NewGuid(), Guid.NewGuid(), quantity: 2);

            Assert.That(result, Is.TypeOf<NoContentResult>());
        }
    }
}
