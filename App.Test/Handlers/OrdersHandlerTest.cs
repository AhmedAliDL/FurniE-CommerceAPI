using System.Linq.Expressions;
using System.Text.Json;
using App.Application.Common.Exceptions;
using FluentValidation;
using App.Application.Common.Interfaces.Repos;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
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
using App.Application.Payments.Command.RefundPayment;
using App.Application.Responses;
using App.Domain.Entities;
using App.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace App.Test.Handlers
{
    [TestFixture]
    [Category("Orders")]
    public class OrdersHandlerTest
    {
        private Mock<IUnitOfWork> _unitOfWork = null!;
        private Mock<IBaseRepository<Order>> _orders = null!;
        private Mock<IBaseRepository<OrderItem>> _orderItems = null!;
        private Mock<IBaseRepository<Product>> _products = null!;
        private Mock<IBaseRepository<Payment>> _payments = null!;
        private Mock<IBaseRepository<PaymentTransaction>> _paymentTransactions = null!;
        private Mock<IBaseRepository<CancellationRequest>> _cancellationRequests = null!;
        private Mock<IBaseRepository<ReturnRequest>> _returnRequests = null!;
        private Mock<IBaseRepository<Review>> _reviews = null!;
        private Mock<ICurrentUserService> _currentUserService = null!;
        private Mock<IIdentityService> _identityService = null!;
        private Mock<INotificationService> _notificationService = null!;
        private Mock<IPaymobService> _paymobService = null!;
        private Mock<ISender> _sender = null!;

        private GetUserOrdersHandler _getUserOrdersHandler = null!;
        private GetOrderDetailsHandler _getOrderDetailsHandler = null!;
        private DeleteOrderHandler _deleteOrderHandler = null!;
        private DeleteOrderItemHandler _deleteOrderItemHandler = null!;
        private UpdateOrderStatusHandler _updateOrderStatusHandler = null!;
        private CancelOrderHandler _cancelOrderHandler = null!;
        private RequestOrderCancellationHandler _requestOrderCancellationHandler = null!;
        private ApproveCancellationRequestHandler _approveCancellationRequestHandler = null!;
        private RejectCancellationRequestHandler _rejectCancellationRequestHandler = null!;
        private GetCancellationRequestsHandler _getCancellationRequestsHandler = null!;
        private RequestOrderReturnHandler _requestOrderReturnHandler = null!;
        private AcceptReturnRequestHandler _acceptReturnRequestHandler = null!;
        private ReceiveReturnRequestHandler _receiveReturnRequestHandler = null!;
        private RejectReturnRequestHandler _rejectReturnRequestHandler = null!;
        private ProcessReturnRefundHandler _processReturnRefundHandler = null!;
        private GetReturnRequestsHandler _getReturnRequestsHandler = null!;

        private readonly Guid _userId = Guid.NewGuid();

        [SetUp]
        public void Setup()
        {
            _unitOfWork = new Mock<IUnitOfWork>();
            _orders = new Mock<IBaseRepository<Order>>();
            _orderItems = new Mock<IBaseRepository<OrderItem>>();
            _products = new Mock<IBaseRepository<Product>>();
            _payments = new Mock<IBaseRepository<Payment>>();
            _paymentTransactions = new Mock<IBaseRepository<PaymentTransaction>>();
            _cancellationRequests = new Mock<IBaseRepository<CancellationRequest>>();
            _returnRequests = new Mock<IBaseRepository<ReturnRequest>>();
            _reviews = new Mock<IBaseRepository<Review>>();
            _currentUserService = new Mock<ICurrentUserService>();
            _identityService = new Mock<IIdentityService>();
            _notificationService = new Mock<INotificationService>();
            _paymobService = new Mock<IPaymobService>();
            _sender = new Mock<ISender>();

            _unitOfWork.Setup(u => u.Orders).Returns(_orders.Object);
            _unitOfWork.Setup(u => u.OrderItems).Returns(_orderItems.Object);
            _unitOfWork.Setup(u => u.Products).Returns(_products.Object);
            _unitOfWork.Setup(u => u.Payments).Returns(_payments.Object);
            _unitOfWork.Setup(u => u.PaymentTransactions).Returns(_paymentTransactions.Object);
            _unitOfWork.Setup(u => u.CancellationRequests).Returns(_cancellationRequests.Object);
            _unitOfWork.Setup(u => u.ReturnRequests).Returns(_returnRequests.Object);
            _unitOfWork.Setup(u => u.Reviews).Returns(_reviews.Object);

            _currentUserService.SetupGet(c => c.UserId).Returns(_userId);
            _notificationService.Setup(n => n.NotifyUserAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _notificationService.Setup(n => n.NotifyAdminAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            _getUserOrdersHandler = new GetUserOrdersHandler(_unitOfWork.Object, _currentUserService.Object, new Mock<ILogger<GetUserOrdersHandler>>().Object);
            _getOrderDetailsHandler = new GetOrderDetailsHandler(_unitOfWork.Object, _currentUserService.Object, new Mock<ILogger<GetOrderDetailsHandler>>().Object);
            _deleteOrderHandler = new DeleteOrderHandler(_unitOfWork.Object, _currentUserService.Object, new Mock<ILogger<DeleteOrderHandler>>().Object);
            _deleteOrderItemHandler = new DeleteOrderItemHandler(_unitOfWork.Object, _currentUserService.Object, new Mock<ILogger<DeleteOrderItemHandler>>().Object);
            _updateOrderStatusHandler = new UpdateOrderStatusHandler(_unitOfWork.Object, new Mock<ILogger<UpdateOrderStatusHandler>>().Object);
            _cancelOrderHandler = new CancelOrderHandler(_unitOfWork.Object, _currentUserService.Object, new Mock<ILogger<CancelOrderHandler>>().Object);
            _requestOrderCancellationHandler = new RequestOrderCancellationHandler(_unitOfWork.Object, _currentUserService.Object, _identityService.Object, _notificationService.Object, new Mock<ILogger<RequestOrderCancellationHandler>>().Object);
            _approveCancellationRequestHandler = new ApproveCancellationRequestHandler(_unitOfWork.Object, _currentUserService.Object, _paymobService.Object, _notificationService.Object, new Mock<ILogger<ApproveCancellationRequestHandler>>().Object);
            _rejectCancellationRequestHandler = new RejectCancellationRequestHandler(_unitOfWork.Object, _currentUserService.Object, _notificationService.Object, new Mock<ILogger<RejectCancellationRequestHandler>>().Object);
            _getCancellationRequestsHandler = new GetCancellationRequestsHandler(_unitOfWork.Object, new Mock<ILogger<GetCancellationRequestsHandler>>().Object);
            _requestOrderReturnHandler = new RequestOrderReturnHandler(_unitOfWork.Object, _currentUserService.Object, _identityService.Object, _notificationService.Object, new Mock<ILogger<RequestOrderReturnHandler>>().Object);
            _acceptReturnRequestHandler = new AcceptReturnRequestHandler(_unitOfWork.Object, _currentUserService.Object, _notificationService.Object, new Mock<ILogger<AcceptReturnRequestHandler>>().Object);
            _receiveReturnRequestHandler = new ReceiveReturnRequestHandler(_unitOfWork.Object, _currentUserService.Object, _notificationService.Object, new Mock<ILogger<ReceiveReturnRequestHandler>>().Object);
            _rejectReturnRequestHandler = new RejectReturnRequestHandler(_unitOfWork.Object, _currentUserService.Object, _notificationService.Object, new Mock<ILogger<RejectReturnRequestHandler>>().Object);
            _processReturnRefundHandler = new ProcessReturnRefundHandler(_unitOfWork.Object, _currentUserService.Object, _sender.Object, _notificationService.Object, new Mock<ILogger<ProcessReturnRefundHandler>>().Object);
            _getReturnRequestsHandler = new GetReturnRequestsHandler(_unitOfWork.Object, new Mock<ILogger<GetReturnRequestsHandler>>().Object);
        }

        private Order ValidOrder(OrderStatus status = OrderStatus.Processing) => new()
        {
            OrderId = Guid.NewGuid(),
            UserId = _userId,
            TotalPrice = 350m,
            ShippingFee = 50m,
            ShippingAddress = "12 Tahrir Square",
            OrderStatus = status
        };

        #region GetUserOrders

        [Test]
        public async Task EnsureGetUserOrdersHandlerReturnsOrdersForCurrentUser()
        {
            var order = ValidOrder();
            _orders.Setup(o => o.FindPagedAsync(
                    It.IsAny<Expression<Func<Order, bool>>>(),
                    It.IsAny<Expression<Func<Order, object>>[]>(),
                    It.IsAny<App.Application.Common.Enums.SortDirection>(),
                    It.IsAny<Expression<Func<Order, object>>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(((IReadOnlyList<Order>)[order], 1));
            _orderItems.Setup(oi => oi.FindAllAsync(It.IsAny<Expression<Func<OrderItem, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<OrderItem>
                {
                    new() { OrderItemId = Guid.NewGuid(), OrderId = order.OrderId, Quantity = 2 },
                    new() { OrderItemId = Guid.NewGuid(), OrderId = order.OrderId, Quantity = 1 }
                });

            var result = await _getUserOrdersHandler.Handle(new GetUserOrdersQuery(), It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.TotalCount, Is.EqualTo(1));
                Assert.That(result.Items, Has.Count.EqualTo(1));
                Assert.That(result.Items[0].OrderId, Is.EqualTo(order.OrderId));
                Assert.That(result.Items[0].Address, Is.EqualTo("12 Tahrir Square"));
                Assert.That(result.Items[0].TotalQuantity, Is.EqualTo(3));
                Assert.That(result.Items[0].TotalPrice, Is.EqualTo(350m));
                Assert.That(result.Items[0].ShippingStatus, Is.EqualTo("Processing"));
                Assert.That(result.Items[0].OrderDate, Is.EqualTo(order.CreatedAt.Date));
                Assert.That(result.Items[0].RemainingDays, Is.EqualTo(5));
            });
        }

        [Test]
        public void EnsureGetUserOrdersHandlerThrowsNotFoundWhenUserIsNull()
        {
            _currentUserService.SetupGet(c => c.UserId).Returns((Guid?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _getUserOrdersHandler.Handle(new GetUserOrdersQuery(), It.IsAny<CancellationToken>()));
        }

        #endregion

        #region GetOrderDetails

        [Test]
        public async Task EnsureGetOrderDetailsHandlerReturnsReviewedItemsWhenReviewExists()
        {
            var order = ValidOrder();
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);
            _orderItems.Setup(oi => oi.FindAllAsync(It.IsAny<Expression<Func<OrderItem, bool>>>(), It.IsAny<Expression<Func<OrderItem, object>>[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<OrderItem>
                {
                    new() { OrderItemId = Guid.NewGuid(), OrderId = order.OrderId, ProductId = Guid.NewGuid(), UnitPrice = 150m, Quantity = 2, Product = new Product { ProductName = "Sofa", ImageUrl = "sofa.jpg" } }
                });
            _reviews.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Review, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Review());

            var result = await _getOrderDetailsHandler.Handle(new GetOrderDetailsQuery(order.OrderId), It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result, Has.Count.EqualTo(1));
                Assert.That(result[0].OrderItemId, Is.Not.EqualTo(Guid.Empty));
                Assert.That(result[0].ProductName, Is.EqualTo("Sofa"));
                Assert.That(result[0].ProductImage, Is.EqualTo("sofa.jpg"));
                Assert.That(result[0].ProductPrice, Is.EqualTo(150m));
                Assert.That(result[0].ProductQuantity, Is.EqualTo(2));
                Assert.That(result[0].ShippingStatus, Is.EqualTo("Processing"));
                Assert.That(result[0].IsReviewed, Is.True);
            });
        }

        [Test]
        public async Task EnsureGetOrderDetailsHandlerReturnsUnreviewedItemsWhenNoReviewExists()
        {
            var order = ValidOrder();
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);
            _orderItems.Setup(oi => oi.FindAllAsync(It.IsAny<Expression<Func<OrderItem, bool>>>(), It.IsAny<Expression<Func<OrderItem, object>>[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<OrderItem>
                {
                    new() { OrderItemId = Guid.NewGuid(), OrderId = order.OrderId, ProductId = Guid.NewGuid(), UnitPrice = 150m, Quantity = 2, Product = new Product { ProductName = "Sofa" } }
                });
            _reviews.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Review, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Review?)null);

            var result = await _getOrderDetailsHandler.Handle(new GetOrderDetailsQuery(order.OrderId), It.IsAny<CancellationToken>());

            Assert.That(result[0].IsReviewed, Is.False);
        }

        [Test]
        public void EnsureGetOrderDetailsHandlerThrowsNotFoundWhenOrderNotFound()
        {
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Order?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _getOrderDetailsHandler.Handle(new GetOrderDetailsQuery(Guid.NewGuid()), It.IsAny<CancellationToken>()));
        }

        [Test]
        public void EnsureGetOrderDetailsHandlerThrowsForbiddenWhenOrderBelongsToAnotherUser()
        {
            var order = ValidOrder();
            order.UserId = Guid.NewGuid();
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            Assert.ThrowsAsync<ForbiddenException>(async () =>
                await _getOrderDetailsHandler.Handle(new GetOrderDetailsQuery(order.OrderId), It.IsAny<CancellationToken>()));
        }

        [Test]
        public async Task EnsureGetOrderDetailsHandlerAllowsAdminToViewAnotherUsersOrder()
        {
            var order = ValidOrder();
            order.UserId = Guid.NewGuid();
            _currentUserService.Setup(c => c.IsInRole("Admin")).Returns(true);
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);
            _orderItems.Setup(oi => oi.FindAllAsync(It.IsAny<Expression<Func<OrderItem, bool>>>(), It.IsAny<Expression<Func<OrderItem, object>>[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<OrderItem>());

            var result = await _getOrderDetailsHandler.Handle(new GetOrderDetailsQuery(order.OrderId), It.IsAny<CancellationToken>());

            Assert.That(result, Is.Empty);
        }

        #endregion

        #region DeleteOrder

        [Test]
        public async Task EnsureDeleteOrderHandlerDeletesOrderAndRestoresStockWhenOrderExists()
        {
            var order = ValidOrder();
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            var orderItems = new List<OrderItem> { new() { OrderItemId = Guid.NewGuid(), OrderId = order.OrderId, ProductId = Guid.NewGuid(), Quantity = 2 } };
            _orderItems.Setup(oi => oi.FindAllAsync(It.IsAny<Expression<Func<OrderItem, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(orderItems);
            _products.Setup(p => p.FindAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Product { ProductId = orderItems[0].ProductId, StockQuantity = 5 });

            await _deleteOrderHandler.Handle(new DeleteOrderCommand(order.OrderId), It.IsAny<CancellationToken>());

            _products.Verify(p => p.Update(It.Is<Product>(prod => prod.StockQuantity == 7)), Times.Once);
            _orderItems.Verify(oi => oi.DeleteRange(orderItems), Times.Once);
            _orders.Verify(o => o.Delete(order), Times.Once);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void EnsureDeleteOrderHandlerThrowsNotFoundWhenOrderNotFound()
        {
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Order?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _deleteOrderHandler.Handle(new DeleteOrderCommand(Guid.NewGuid()), It.IsAny<CancellationToken>()));
        }

        [Test]
        public void EnsureDeleteOrderHandlerThrowsForbiddenWhenOrderBelongsToAnotherUser()
        {
            var order = ValidOrder();
            order.UserId = Guid.NewGuid();
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            Assert.ThrowsAsync<ForbiddenException>(async () =>
                await _deleteOrderHandler.Handle(new DeleteOrderCommand(order.OrderId), It.IsAny<CancellationToken>()));
        }

        [TestCase(OrderStatus.Cancelled)]
        [TestCase(OrderStatus.CancellationRequested)]
        public void EnsureDeleteOrderHandlerThrowsConflictWhenStockWasAlreadyRestored(OrderStatus status)
        {
            var order = ValidOrder(status);
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            Assert.ThrowsAsync<ConflictException>(async () =>
                await _deleteOrderHandler.Handle(new DeleteOrderCommand(order.OrderId), It.IsAny<CancellationToken>()));
        }

        [Test]
        public void EnsureDeleteOrderHandlerThrowsConflictWhenOrderIsPaid()
        {
            var order = ValidOrder();
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);
            _payments.Setup(p => p.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Payment { OrderId = order.OrderId, PaymentStatus = "Completed" });

            Assert.ThrowsAsync<ConflictException>(async () =>
                await _deleteOrderHandler.Handle(new DeleteOrderCommand(order.OrderId), It.IsAny<CancellationToken>()));
        }

        [Test]
        public void EnsureDeleteOrderHandlerThrowsConflictWhenCancellationRequestIsActive()
        {
            var order = ValidOrder();
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);
            _cancellationRequests.Setup(cr => cr.FindAsync(It.IsAny<Expression<Func<CancellationRequest, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CancellationRequest { OrderId = order.OrderId, Status = CancellationRequestStatus.Pending });

            Assert.ThrowsAsync<ConflictException>(async () =>
                await _deleteOrderHandler.Handle(new DeleteOrderCommand(order.OrderId), It.IsAny<CancellationToken>()));
        }

        [Test]
        public void EnsureDeleteOrderHandlerThrowsConflictWhenReturnRequestIsActive()
        {
            var order = ValidOrder();
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);
            _returnRequests.Setup(rr => rr.FindAsync(It.IsAny<Expression<Func<ReturnRequest, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReturnRequest { OrderId = order.OrderId, Status = ReturnRequestStatus.Pending });

            Assert.ThrowsAsync<ConflictException>(async () =>
                await _deleteOrderHandler.Handle(new DeleteOrderCommand(order.OrderId), It.IsAny<CancellationToken>()));
        }

        #endregion

        #region DeleteOrderItem

        [Test]
        public async Task EnsureDeleteOrderItemHandlerRemovesQuantityWhenOrderHasMoreItems()
        {
            var order = ValidOrder();
            var productId = Guid.NewGuid();
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);
            _orderItems.Setup(oi => oi.FindAllAsync(It.IsAny<Expression<Func<OrderItem, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<OrderItem> { new() { OrderItemId = Guid.NewGuid(), OrderId = order.OrderId, ProductId = productId, Quantity = 5, UnitPrice = 100m } });
            _products.Setup(p => p.FindAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Product { ProductId = productId, StockQuantity = 5 });

            await _deleteOrderItemHandler.Handle(new DeleteOrderItemCommand(order.OrderId, productId, 2), It.IsAny<CancellationToken>());

            _orderItems.Verify(oi => oi.Update(It.Is<OrderItem>(item => item.Quantity == 3)), Times.Once);
            _orders.Verify(o => o.Update(It.Is<Order>(ord => ord.TotalPrice == 150m)), Times.Once);
            _products.Verify(p => p.Update(It.Is<Product>(prod => prod.StockQuantity == 7)), Times.Once);
            _orderItems.Verify(oi => oi.Delete(It.IsAny<OrderItem>()), Times.Never);
            _orders.Verify(o => o.Delete(It.IsAny<Order>()), Times.Never);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task EnsureDeleteOrderItemHandlerDeletesWholeOrderWhenLastItemIsRemoved()
        {
            var order = ValidOrder();
            var productId = Guid.NewGuid();
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);
            _orderItems.Setup(oi => oi.FindAllAsync(It.IsAny<Expression<Func<OrderItem, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<OrderItem> { new() { OrderItemId = Guid.NewGuid(), OrderId = order.OrderId, ProductId = productId, Quantity = 2, UnitPrice = 100m } });
            _products.Setup(p => p.FindAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Product { ProductId = productId, StockQuantity = 5 });

            await _deleteOrderItemHandler.Handle(new DeleteOrderItemCommand(order.OrderId, productId, 2), It.IsAny<CancellationToken>());

            _orders.Verify(o => o.Delete(order), Times.Once);
            _orderItems.Verify(oi => oi.Delete(It.IsAny<OrderItem>()), Times.Once);
        }

        [Test]
        public async Task EnsureDeleteOrderItemHandlerDeletesOnlyItemWhenOtherItemsRemain()
        {
            var order = ValidOrder();
            var productId = Guid.NewGuid();
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);
            _orderItems.Setup(oi => oi.FindAllAsync(It.IsAny<Expression<Func<OrderItem, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<OrderItem>
                {
                    new() { OrderItemId = Guid.NewGuid(), OrderId = order.OrderId, ProductId = productId, Quantity = 2, UnitPrice = 100m },
                    new() { OrderItemId = Guid.NewGuid(), OrderId = order.OrderId, ProductId = Guid.NewGuid(), Quantity = 1, UnitPrice = 50m }
                });
            _products.Setup(p => p.FindAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Product { ProductId = productId, StockQuantity = 5 });

            await _deleteOrderItemHandler.Handle(new DeleteOrderItemCommand(order.OrderId, productId, 2), It.IsAny<CancellationToken>());

            _orderItems.Verify(oi => oi.Delete(It.IsAny<OrderItem>()), Times.Once);
            _orderItems.Verify(oi => oi.Update(It.IsAny<OrderItem>()), Times.Never);
            _orders.Verify(o => o.Delete(It.IsAny<Order>()), Times.Never);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void EnsureDeleteOrderItemHandlerThrowsNotFoundWhenOrderNotFound()
        {
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Order?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _deleteOrderItemHandler.Handle(new DeleteOrderItemCommand(Guid.NewGuid(), Guid.NewGuid(), 1), It.IsAny<CancellationToken>()));
        }

        [Test]
        public void EnsureDeleteOrderItemHandlerThrowsNotFoundWhenProductIsNotInOrder()
        {
            var order = ValidOrder();
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);
            _orderItems.Setup(oi => oi.FindAllAsync(It.IsAny<Expression<Func<OrderItem, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<OrderItem> { new() { OrderItemId = Guid.NewGuid(), OrderId = order.OrderId, ProductId = Guid.NewGuid(), Quantity = 5, UnitPrice = 100m } });

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _deleteOrderItemHandler.Handle(new DeleteOrderItemCommand(order.OrderId, Guid.NewGuid(), 1), It.IsAny<CancellationToken>()));
        }

        [Test]
        public void EnsureDeleteOrderItemHandlerThrowsForbiddenWhenOrderBelongsToAnotherUser()
        {
            var order = ValidOrder();
            order.UserId = Guid.NewGuid();
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            Assert.ThrowsAsync<ForbiddenException>(async () =>
                await _deleteOrderItemHandler.Handle(new DeleteOrderItemCommand(order.OrderId, Guid.NewGuid(), 1), It.IsAny<CancellationToken>()));
        }

        [TestCase(OrderStatus.Shipped)]
        [TestCase(OrderStatus.Delivered)]
        [TestCase(OrderStatus.Cancelled)]
        public void EnsureDeleteOrderItemHandlerThrowsConflictWhenOrderCannotBeModified(OrderStatus status)
        {
            var order = ValidOrder(status);
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            Assert.ThrowsAsync<ConflictException>(async () =>
                await _deleteOrderItemHandler.Handle(new DeleteOrderItemCommand(order.OrderId, Guid.NewGuid(), 1), It.IsAny<CancellationToken>()));
        }

        [Test]
        public void EnsureDeleteOrderItemHandlerThrowsConflictWhenOrderIsPaid()
        {
            var order = ValidOrder();
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);
            _payments.Setup(p => p.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Payment { OrderId = order.OrderId, PaymentStatus = "Completed" });

            Assert.ThrowsAsync<ConflictException>(async () =>
                await _deleteOrderItemHandler.Handle(new DeleteOrderItemCommand(order.OrderId, Guid.NewGuid(), 1), It.IsAny<CancellationToken>()));
        }

        [Test]
        public void EnsureDeleteOrderItemHandlerThrowsValidationWhenQuantityExceedsOrderedQuantity()
        {
            var order = ValidOrder();
            var productId = Guid.NewGuid();
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);
            _orderItems.Setup(oi => oi.FindAllAsync(It.IsAny<Expression<Func<OrderItem, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<OrderItem> { new() { OrderItemId = Guid.NewGuid(), OrderId = order.OrderId, ProductId = productId, Quantity = 2, UnitPrice = 100m } });

            Assert.ThrowsAsync<ValidationException>(async () =>
                await _deleteOrderItemHandler.Handle(new DeleteOrderItemCommand(order.OrderId, productId, 3), It.IsAny<CancellationToken>()));
            _products.Verify(p => p.Update(It.IsAny<Product>()), Times.Never);
        }

        #endregion

        #region UpdateOrderStatus

        [Test]
        public void EnsureUpdateOrderStatusCommandAcceptsStringEnumValue()
        {
            var orderId = Guid.NewGuid();

            var command = JsonSerializer.Deserialize<UpdateOrderStatusCommand>(
                $"{{\"OrderId\":\"{orderId}\",\"Status\":\"Shipped\"}}");

            Assert.Multiple(() =>
            {
                Assert.That(command!.OrderId, Is.EqualTo(orderId));
                Assert.That(command.Status, Is.EqualTo(OrderStatus.Shipped));
            });
        }

        [Test]
        public async Task EnsureUpdateOrderStatusHandlerUpdatesStatusWhenTransitionIsValid()
        {
            var order = ValidOrder(OrderStatus.Pending);
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            await _updateOrderStatusHandler.Handle(new UpdateOrderStatusCommand(order.OrderId, OrderStatus.Confirmed), It.IsAny<CancellationToken>());

            Assert.That(order.UpdatedAt, Is.Not.Null);
            _orders.Verify(o => o.Update(It.Is<Order>(ord => ord.OrderStatus == OrderStatus.Confirmed)), Times.Once);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void EnsureUpdateOrderStatusHandlerThrowsInvalidOperationWhenTransitionIsNotAllowed()
        {
            var order = ValidOrder(OrderStatus.Delivered);
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _updateOrderStatusHandler.Handle(new UpdateOrderStatusCommand(order.OrderId, OrderStatus.Shipped), It.IsAny<CancellationToken>()));
        }

        [Test]
        public void EnsureUpdateOrderStatusHandlerThrowsNotFoundWhenOrderNotFound()
        {
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Order?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _updateOrderStatusHandler.Handle(new UpdateOrderStatusCommand(Guid.NewGuid(), OrderStatus.Confirmed), It.IsAny<CancellationToken>()));
        }

        #endregion

        #region CancelOrder

        [Test]
        public async Task EnsureCancelOrderHandlerCancelsUnpaidOrderAndRestoresStock()
        {
            var order = ValidOrder(OrderStatus.Processing);
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);
            _payments.Setup(p => p.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Payment?)null);
            _orderItems.Setup(oi => oi.FindAllAsync(It.IsAny<Expression<Func<OrderItem, bool>>>(), It.IsAny<Expression<Func<OrderItem, object>>[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<OrderItem>
                {
                    new() { OrderItemId = Guid.NewGuid(), OrderId = order.OrderId, Quantity = 2, Product = new Product { StockQuantity = 0 } }
                });

            await _cancelOrderHandler.Handle(new CancelOrderCommand(order.OrderId), It.IsAny<CancellationToken>());

            _orders.Verify(o => o.Update(It.Is<Order>(ord => ord.OrderStatus == OrderStatus.Cancelled)), Times.Once);
            _products.Verify(p => p.Update(It.Is<Product>(prod => prod.StockQuantity == 2 && prod.IsActive)), Times.Once);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task EnsureCancelOrderHandlerCancelsPendingPaymentWhenOrderIsCancelled()
        {
            var order = ValidOrder(OrderStatus.Processing);
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);
            _payments.Setup(p => p.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Payment { OrderId = order.OrderId, PaymentStatus = "Pending" });
            _orderItems.Setup(oi => oi.FindAllAsync(It.IsAny<Expression<Func<OrderItem, bool>>>(), It.IsAny<Expression<Func<OrderItem, object>>[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<OrderItem>());

            await _cancelOrderHandler.Handle(new CancelOrderCommand(order.OrderId), It.IsAny<CancellationToken>());

            _payments.Verify(p => p.Update(It.Is<Payment>(pay => pay.PaymentStatus == "Cancelled")), Times.Once);
        }

        [Test]
        public void EnsureCancelOrderHandlerThrowsForbiddenWhenOrderBelongsToAnotherUser()
        {
            var order = ValidOrder(OrderStatus.Processing);
            order.UserId = Guid.NewGuid();
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            Assert.ThrowsAsync<ForbiddenException>(async () =>
                await _cancelOrderHandler.Handle(new CancelOrderCommand(order.OrderId), It.IsAny<CancellationToken>()));
        }

        [Test]
        public void EnsureCancelOrderHandlerThrowsInvalidOperationWhenOrderIsDelivered()
        {
            var order = ValidOrder(OrderStatus.Delivered);
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _cancelOrderHandler.Handle(new CancelOrderCommand(order.OrderId), It.IsAny<CancellationToken>()));
        }

        [Test]
        public void EnsureCancelOrderHandlerThrowsInvalidOperationWhenOrderIsAlreadyPaid()
        {
            var order = ValidOrder(OrderStatus.Processing);
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);
            _payments.Setup(p => p.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Payment { OrderId = order.OrderId, PaymentStatus = "Completed" });

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _cancelOrderHandler.Handle(new CancelOrderCommand(order.OrderId), It.IsAny<CancellationToken>()));
        }

        [Test]
        public void EnsureCancelOrderHandlerThrowsNotFoundWhenOrderNotFound()
        {
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Order?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _cancelOrderHandler.Handle(new CancelOrderCommand(Guid.NewGuid()), It.IsAny<CancellationToken>()));
        }

        #endregion

        #region RequestOrderCancellation

        [Test]
        public async Task EnsureRequestOrderCancellationHandlerCreatesRequestWhenOrderIsPaid()
        {
            var order = ValidOrder(OrderStatus.Processing);
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);
            _payments.Setup(p => p.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Payment { OrderId = order.OrderId, PaymentStatus = "Completed" });
            _cancellationRequests.Setup(cr => cr.FindAsync(It.IsAny<Expression<Func<CancellationRequest, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((CancellationRequest?)null);
            var adminId = Guid.NewGuid();
            _identityService.Setup(i => i.GetUsersInRoleAsync("Admin"))
                .ReturnsAsync(new List<AppUser> { new() { Id = adminId } });

            var result = await _requestOrderCancellationHandler.Handle(new RequestOrderCancellationCommand(order.OrderId, "Ordered by mistake"), It.IsAny<CancellationToken>());

            Assert.That(result.IsSuccess, Is.True);
            _cancellationRequests.Verify(cr => cr.AddAsync(It.Is<CancellationRequest>(req =>
                req.OrderId == order.OrderId &&
                req.CustomerId == _userId &&
                req.Reason == "Ordered by mistake" &&
                req.PreviousOrderStatus == "Processing" &&
                req.Status == CancellationRequestStatus.Pending), It.IsAny<CancellationToken>()), Times.Once);
            _orders.Verify(o => o.Update(It.Is<Order>(ord => ord.OrderStatus == OrderStatus.CancellationRequested)), Times.Once);
            _notificationService.Verify(n => n.NotifyAdminAsync(adminId, It.IsAny<string>(), _userId, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task EnsureRequestOrderCancellationHandlerReturnsFailureWhenOrderIsNotPaid()
        {
            var order = ValidOrder(OrderStatus.Processing);
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);
            _payments.Setup(p => p.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Payment?)null);

            var result = await _requestOrderCancellationHandler.Handle(new RequestOrderCancellationCommand(order.OrderId, "Ordered by mistake"), It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Errors, Has.Some.EqualTo("This order is not paid yet. You can cancel it directly from your orders list."));
            });
            _cancellationRequests.Verify(cr => cr.AddAsync(It.IsAny<CancellationRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task EnsureRequestOrderCancellationHandlerReturnsFailureWhenPendingRequestAlreadyExists()
        {
            var order = ValidOrder(OrderStatus.Processing);
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);
            _payments.Setup(p => p.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Payment { OrderId = order.OrderId, PaymentStatus = "Completed" });
            _cancellationRequests.Setup(cr => cr.FindAsync(It.IsAny<Expression<Func<CancellationRequest, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CancellationRequest { OrderId = order.OrderId, Status = CancellationRequestStatus.Pending });

            var result = await _requestOrderCancellationHandler.Handle(new RequestOrderCancellationCommand(order.OrderId, "Ordered by mistake"), It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Errors, Has.Some.EqualTo("A cancellation request is already pending for this order."));
            });
        }

        [Test]
        public async Task EnsureRequestOrderCancellationHandlerReturnsFailureWhenOrderCanNoLongerBeCancelled()
        {
            var order = ValidOrder(OrderStatus.Delivered);
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            var result = await _requestOrderCancellationHandler.Handle(new RequestOrderCancellationCommand(order.OrderId, "Too late"), It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Errors, Has.Some.EqualTo("An order in status Delivered can no longer be cancelled."));
            });
        }

        [Test]
        public void EnsureRequestOrderCancellationHandlerThrowsForbiddenWhenOrderBelongsToAnotherUser()
        {
            var order = ValidOrder(OrderStatus.Processing);
            order.UserId = Guid.NewGuid();
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            Assert.ThrowsAsync<ForbiddenException>(async () =>
                await _requestOrderCancellationHandler.Handle(new RequestOrderCancellationCommand(order.OrderId, "Not mine"), It.IsAny<CancellationToken>()));
        }

        [Test]
        public void EnsureRequestOrderCancellationHandlerThrowsNotFoundWhenOrderNotFound()
        {
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Order?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _requestOrderCancellationHandler.Handle(new RequestOrderCancellationCommand(Guid.NewGuid(), "Reason"), It.IsAny<CancellationToken>()));
        }

        #endregion

        #region ApproveCancellationRequest

        private CancellationRequest ValidCancellationRequest(CancellationRequestStatus status = CancellationRequestStatus.Pending) => new()
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            CustomerId = _userId,
            Reason = "Ordered by mistake",
            PreviousOrderStatus = "Processing",
            Status = status,
            Order = ValidOrder(OrderStatus.CancellationRequested)
        };

        [Test]
        public async Task EnsureApproveCancellationRequestHandlerApprovesRequestWhenVoidSucceeds()
        {
            var cancellationRequest = ValidCancellationRequest();
            _cancellationRequests.Setup(cr => cr.FindAsync(It.IsAny<Expression<Func<CancellationRequest, bool>>>(), It.IsAny<Expression<Func<CancellationRequest, object>>[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(cancellationRequest);
            _paymentTransactions.Setup(pt => pt.FindAllAsync(It.IsAny<Expression<Func<PaymentTransaction, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<PaymentTransaction> { new() { OrderId = cancellationRequest.OrderId, PaymobTransactionId = 123456, Status = "Success", PaymobCreatedAt = DateTime.UtcNow } });
            _paymobService.Setup(p => p.VoidTransactionAsync(123456, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new App.Application.Payments.Dto.Paymob.PaymobOperationResult { Success = true });
            _payments.Setup(p => p.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Payment { OrderId = cancellationRequest.OrderId, PaymentStatus = "Completed" });
            _orderItems.Setup(oi => oi.FindAllAsync(It.IsAny<Expression<Func<OrderItem, bool>>>(), It.IsAny<Expression<Func<OrderItem, object>>[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<OrderItem>
                {
                    new() { OrderItemId = Guid.NewGuid(), OrderId = cancellationRequest.OrderId, Quantity = 2, Product = new Product { StockQuantity = 0 } }
                });

            var result = await _approveCancellationRequestHandler.Handle(new ApproveCancellationRequestCommand(cancellationRequest.Id), It.IsAny<CancellationToken>());

            Assert.That(result.IsSuccess, Is.True);
            _paymentTransactions.Verify(pt => pt.Update(It.Is<PaymentTransaction>(t => t.Status == "Voided")), Times.Once);
            _payments.Verify(p => p.Update(It.Is<Payment>(pay => pay.PaymentStatus == "Voided")), Times.Once);
            _cancellationRequests.Verify(cr => cr.Update(It.Is<CancellationRequest>(req => req.Status == CancellationRequestStatus.Approved && req.ProcessedAt != null)), Times.Once);
            _orders.Verify(o => o.Update(It.Is<Order>(ord => ord.OrderStatus == OrderStatus.Cancelled)), Times.Once);
            _products.Verify(p => p.Update(It.Is<Product>(prod => prod.StockQuantity == 2 && prod.IsActive)), Times.Once);
            _notificationService.Verify(n => n.NotifyUserAsync(cancellationRequest.Order.UserId, It.IsAny<string>(), _userId, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task EnsureApproveCancellationRequestHandlerReturnsFailureWhenRequestIsAlreadyProcessed()
        {
            var cancellationRequest = ValidCancellationRequest(CancellationRequestStatus.Approved);
            _cancellationRequests.Setup(cr => cr.FindAsync(It.IsAny<Expression<Func<CancellationRequest, bool>>>(), It.IsAny<Expression<Func<CancellationRequest, object>>[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(cancellationRequest);

            var result = await _approveCancellationRequestHandler.Handle(new ApproveCancellationRequestCommand(cancellationRequest.Id), It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Errors[0], Does.Contain("Cancellation request is already Approved"));
            });
            _paymobService.Verify(p => p.VoidTransactionAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task EnsureApproveCancellationRequestHandlerReturnsFailureWhenNoSuccessfulTransactionExists()
        {
            var cancellationRequest = ValidCancellationRequest();
            _cancellationRequests.Setup(cr => cr.FindAsync(It.IsAny<Expression<Func<CancellationRequest, bool>>>(), It.IsAny<Expression<Func<CancellationRequest, object>>[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(cancellationRequest);
            _paymentTransactions.Setup(pt => pt.FindAllAsync(It.IsAny<Expression<Func<PaymentTransaction, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<PaymentTransaction> { new() { OrderId = cancellationRequest.OrderId, PaymobTransactionId = 123456, Status = "Failed" } });

            var result = await _approveCancellationRequestHandler.Handle(new ApproveCancellationRequestCommand(cancellationRequest.Id), It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Errors, Has.Some.EqualTo("No successful payment found for this order. Use the direct cancel or refund flow instead."));
            });
            _paymobService.Verify(p => p.VoidTransactionAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task EnsureApproveCancellationRequestHandlerReturnsFailureWhenPaymobRejectsTheVoid()
        {
            var cancellationRequest = ValidCancellationRequest();
            _cancellationRequests.Setup(cr => cr.FindAsync(It.IsAny<Expression<Func<CancellationRequest, bool>>>(), It.IsAny<Expression<Func<CancellationRequest, object>>[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(cancellationRequest);
            _paymentTransactions.Setup(pt => pt.FindAllAsync(It.IsAny<Expression<Func<PaymentTransaction, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<PaymentTransaction> { new() { OrderId = cancellationRequest.OrderId, PaymobTransactionId = 123456, Status = "Success" } });
            _paymobService.Setup(p => p.VoidTransactionAsync(123456, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new App.Application.Payments.Dto.Paymob.PaymobOperationResult { Success = false, Message = "Transaction already settled" });

            var result = await _approveCancellationRequestHandler.Handle(new ApproveCancellationRequestCommand(cancellationRequest.Id), It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Errors, Has.Some.EqualTo("Transaction already settled"));
            });
            _cancellationRequests.Verify(cr => cr.Update(It.IsAny<CancellationRequest>()), Times.Never);
        }

        [Test]
        public void EnsureApproveCancellationRequestHandlerThrowsNotFoundWhenRequestNotFound()
        {
            _cancellationRequests.Setup(cr => cr.FindAsync(It.IsAny<Expression<Func<CancellationRequest, bool>>>(), It.IsAny<Expression<Func<CancellationRequest, object>>[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((CancellationRequest?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _approveCancellationRequestHandler.Handle(new ApproveCancellationRequestCommand(Guid.NewGuid()), It.IsAny<CancellationToken>()));
        }

        #endregion

        #region RejectCancellationRequest

        [Test]
        public async Task EnsureRejectCancellationRequestHandlerRejectsPendingRequestAndRestoresPreviousStatus()
        {
            var cancellationRequest = ValidCancellationRequest();
            _cancellationRequests.Setup(cr => cr.FindAsync(It.IsAny<Expression<Func<CancellationRequest, bool>>>(), It.IsAny<Expression<Func<CancellationRequest, object>>[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(cancellationRequest);

            var result = await _rejectCancellationRequestHandler.Handle(new RejectCancellationRequestCommand(cancellationRequest.Id, "Reason is not valid"), It.IsAny<CancellationToken>());

            Assert.That(result.IsSuccess, Is.True);
            _cancellationRequests.Verify(cr => cr.Update(It.Is<CancellationRequest>(req =>
                req.Status == CancellationRequestStatus.Rejected &&
                req.AdminNote == "Reason is not valid" &&
                req.ProcessedAt != null)), Times.Once);
            _orders.Verify(o => o.Update(It.Is<Order>(ord => ord.OrderStatus == OrderStatus.Processing)), Times.Once);
            _notificationService.Verify(n => n.NotifyUserAsync(cancellationRequest.Order.UserId, It.IsAny<string>(), _userId, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task EnsureRejectCancellationRequestHandlerReturnsFailureWhenRequestIsAlreadyProcessed()
        {
            var cancellationRequest = ValidCancellationRequest(CancellationRequestStatus.Rejected);
            _cancellationRequests.Setup(cr => cr.FindAsync(It.IsAny<Expression<Func<CancellationRequest, bool>>>(), It.IsAny<Expression<Func<CancellationRequest, object>>[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(cancellationRequest);

            var result = await _rejectCancellationRequestHandler.Handle(new RejectCancellationRequestCommand(cancellationRequest.Id, "No"), It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Errors[0], Does.Contain("Cancellation request is already Rejected"));
            });
        }

        [Test]
        public void EnsureRejectCancellationRequestHandlerThrowsNotFoundWhenRequestNotFound()
        {
            _cancellationRequests.Setup(cr => cr.FindAsync(It.IsAny<Expression<Func<CancellationRequest, bool>>>(), It.IsAny<Expression<Func<CancellationRequest, object>>[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((CancellationRequest?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _rejectCancellationRequestHandler.Handle(new RejectCancellationRequestCommand(Guid.NewGuid(), "No"), It.IsAny<CancellationToken>()));
        }

        #endregion

        #region GetCancellationRequests

        [Test]
        public async Task EnsureGetCancellationRequestsHandlerReturnsMappedRequests()
        {
            var cancellationRequest = ValidCancellationRequest();
            cancellationRequest.Customer = new AppUser { Id = _userId, FName = "Ahmed", LName = "Ali" };
            _cancellationRequests.Setup(cr => cr.FindPagedAsync(
                    It.IsAny<Expression<Func<CancellationRequest, bool>>>(),
                    It.IsAny<Expression<Func<CancellationRequest, object>>[]>(),
                    It.IsAny<App.Application.Common.Enums.SortDirection>(),
                    It.IsAny<Expression<Func<CancellationRequest, object>>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(((IReadOnlyList<CancellationRequest>)[cancellationRequest], 1));

            var result = await _getCancellationRequestsHandler.Handle(new GetCancellationRequestsQuery(null), It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.True);
                Assert.That(result.Value!.TotalCount, Is.EqualTo(1));
                Assert.That(result.Value.Items, Has.Count.EqualTo(1));
                Assert.That(result.Value.Items[0].Id, Is.EqualTo(cancellationRequest.Id));
                Assert.That(result.Value.Items[0].OrderId, Is.EqualTo(cancellationRequest.OrderId));
                Assert.That(result.Value.Items[0].CustomerName, Is.EqualTo("Ahmed Ali"));
                Assert.That(result.Value.Items[0].Reason, Is.EqualTo("Ordered by mistake"));
                Assert.That(result.Value.Items[0].Status, Is.EqualTo(CancellationRequestStatus.Pending));
            });
        }

        #endregion

        #region RequestOrderReturn

        private Order DeliveredOrder() 
        {
            var order = ValidOrder(OrderStatus.Delivered);
            order.UpdatedAt = DateTime.UtcNow;
            return order;
        }

        [Test]
        public async Task EnsureRequestOrderReturnHandlerCreatesRequestWhenOrderIsDelivered()
        {
            var order = DeliveredOrder();
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);
            _returnRequests.Setup(rr => rr.FindAsync(It.IsAny<Expression<Func<ReturnRequest, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ReturnRequest?)null);
            var orderItemId = Guid.NewGuid();
            _orderItems.Setup(oi => oi.FindAllAsync(It.IsAny<Expression<Func<OrderItem, bool>>>(), It.IsAny<Expression<Func<OrderItem, object>>[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<OrderItem>
                {
                    new() { OrderItemId = orderItemId, OrderId = order.OrderId, Quantity = 2, Product = new Product { ProductName = "Sofa" } }
                });
            var adminId = Guid.NewGuid();
            _identityService.Setup(i => i.GetUsersInRoleAsync("Admin"))
                .ReturnsAsync(new List<AppUser> { new() { Id = adminId } });

            var request = new RequestOrderReturnCommand(order.OrderId, "MerchantDefect", "Broken leg", [new ReturnItemRequestDto { OrderItemId = orderItemId, Quantity = 2 }]);

            var result = await _requestOrderReturnHandler.Handle(request, It.IsAny<CancellationToken>());

            Assert.That(result.IsSuccess, Is.True);
            _returnRequests.Verify(rr => rr.AddAsync(It.Is<ReturnRequest>(req =>
                req.OrderId == order.OrderId &&
                req.CustomerId == _userId &&
                req.ReasonCategory == ReturnReasonCategory.MerchantDefect &&
                req.ReasonText == "Broken leg" &&
                req.Status == ReturnRequestStatus.Pending &&
                req.ReturnItems.Count == 1), It.IsAny<CancellationToken>()), Times.Once);
            _notificationService.Verify(n => n.NotifyAdminAsync(adminId, It.IsAny<string>(), _userId, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task EnsureRequestOrderReturnHandlerReturnsFailureWhenOrderIsNotDelivered()
        {
            var order = ValidOrder(OrderStatus.Shipped);
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            var result = await _requestOrderReturnHandler.Handle(new RequestOrderReturnCommand(order.OrderId, "MerchantDefect", "Broken leg", []), It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Errors, Has.Some.EqualTo("Only delivered orders can be returned."));
            });
            _returnRequests.Verify(rr => rr.AddAsync(It.IsAny<ReturnRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task EnsureRequestOrderReturnHandlerReturnsFailureWhenReturnWindowExpired()
        {
            var order = ValidOrder(OrderStatus.Delivered);
            order.UpdatedAt = DateTime.UtcNow.AddDays(-20);
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            var result = await _requestOrderReturnHandler.Handle(new RequestOrderReturnCommand(order.OrderId, "CustomerChoice", "Changed my mind", []), It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Errors, Has.Some.EqualTo("Returns are only allowed within 14 days after delivery."));
            });
        }

        [Test]
        public async Task EnsureRequestOrderReturnHandlerReturnsFailureWhenActiveRequestAlreadyExists()
        {
            var order = DeliveredOrder();
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);
            _returnRequests.Setup(rr => rr.FindAsync(It.IsAny<Expression<Func<ReturnRequest, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReturnRequest { OrderId = order.OrderId, Status = ReturnRequestStatus.Pending });

            var result = await _requestOrderReturnHandler.Handle(new RequestOrderReturnCommand(order.OrderId, "CustomerChoice", "Changed my mind", []), It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Errors, Has.Some.EqualTo("A return request is already active for this order."));
            });
        }

        [Test]
        public async Task EnsureRequestOrderReturnHandlerReturnsFailureWhenItemDoesNotBelongToOrder()
        {
            var order = DeliveredOrder();
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);
            _returnRequests.Setup(rr => rr.FindAsync(It.IsAny<Expression<Func<ReturnRequest, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ReturnRequest?)null);
            _orderItems.Setup(oi => oi.FindAllAsync(It.IsAny<Expression<Func<OrderItem, bool>>>(), It.IsAny<Expression<Func<OrderItem, object>>[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<OrderItem>
                {
                    new() { OrderItemId = Guid.NewGuid(), OrderId = order.OrderId, Quantity = 2, Product = new Product { ProductName = "Sofa" } }
                });

            var request = new RequestOrderReturnCommand(order.OrderId, "CustomerChoice", "Changed my mind", [new ReturnItemRequestDto { OrderItemId = Guid.NewGuid(), Quantity = 1 }]);

            var result = await _requestOrderReturnHandler.Handle(request, It.IsAny<CancellationToken>());

            Assert.That(result.IsSuccess, Is.False);
            _returnRequests.Verify(rr => rr.AddAsync(It.IsAny<ReturnRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task EnsureRequestOrderReturnHandlerReturnsFailureWhenQuantityExceedsOrderedUnits()
        {
            var order = DeliveredOrder();
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);
            _returnRequests.Setup(rr => rr.FindAsync(It.IsAny<Expression<Func<ReturnRequest, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ReturnRequest?)null);
            var orderItemId = Guid.NewGuid();
            _orderItems.Setup(oi => oi.FindAllAsync(It.IsAny<Expression<Func<OrderItem, bool>>>(), It.IsAny<Expression<Func<OrderItem, object>>[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<OrderItem>
                {
                    new() { OrderItemId = orderItemId, OrderId = order.OrderId, Quantity = 1, Product = new Product { ProductName = "Sofa" } }
                });

            var request = new RequestOrderReturnCommand(order.OrderId, "CustomerChoice", "Changed my mind", [new ReturnItemRequestDto { OrderItemId = orderItemId, Quantity = 2 }]);

            var result = await _requestOrderReturnHandler.Handle(request, It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Errors[0], Does.Contain("only 1 were ordered"));
            });
        }

        [Test]
        public void EnsureRequestOrderReturnHandlerThrowsForbiddenWhenOrderBelongsToAnotherUser()
        {
            var order = DeliveredOrder();
            order.UserId = Guid.NewGuid();
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            Assert.ThrowsAsync<ForbiddenException>(async () =>
                await _requestOrderReturnHandler.Handle(new RequestOrderReturnCommand(order.OrderId, "CustomerChoice", "Not mine", []), It.IsAny<CancellationToken>()));
        }

        #endregion

        #region AcceptReturnRequest

        private ReturnRequest ValidReturnRequest(ReturnRequestStatus status = ReturnRequestStatus.Pending) => new()
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            CustomerId = _userId,
            ReasonCategory = ReturnReasonCategory.CustomerChoice,
            ReasonText = "Changed my mind",
            Status = status
        };

        [Test]
        public async Task EnsureAcceptReturnRequestHandlerAcceptsPendingRequest()
        {
            var returnRequest = ValidReturnRequest();
            _returnRequests.Setup(rr => rr.FindAsync(It.IsAny<Expression<Func<ReturnRequest, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(returnRequest);

            var result = await _acceptReturnRequestHandler.Handle(new AcceptReturnRequestCommand(returnRequest.Id, "Aramex", "01012345678"), It.IsAny<CancellationToken>());

            Assert.That(result.IsSuccess, Is.True);
            _returnRequests.Verify(rr => rr.Update(It.Is<ReturnRequest>(req =>
                req.Status == ReturnRequestStatus.Accepted &&
                req.CourierName == "Aramex" &&
                req.CourierPhone == "01012345678")), Times.Once);
            _notificationService.Verify(n => n.NotifyUserAsync(_userId, It.IsAny<string>(), _userId, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task EnsureAcceptReturnRequestHandlerReturnsFailureWhenRequestIsNotPending()
        {
            var returnRequest = ValidReturnRequest(ReturnRequestStatus.Accepted);
            _returnRequests.Setup(rr => rr.FindAsync(It.IsAny<Expression<Func<ReturnRequest, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(returnRequest);

            var result = await _acceptReturnRequestHandler.Handle(new AcceptReturnRequestCommand(returnRequest.Id, "Aramex", "01012345678"), It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Errors[0], Does.Contain("Return request is already Accepted"));
            });
        }

        [Test]
        public void EnsureAcceptReturnRequestHandlerThrowsNotFoundWhenRequestNotFound()
        {
            _returnRequests.Setup(rr => rr.FindAsync(It.IsAny<Expression<Func<ReturnRequest, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ReturnRequest?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _acceptReturnRequestHandler.Handle(new AcceptReturnRequestCommand(Guid.NewGuid(), "Aramex", "01012345678"), It.IsAny<CancellationToken>()));
        }

        #endregion

        #region ReceiveReturnRequest

        [Test]
        public async Task EnsureReceiveReturnRequestHandlerMarksItemsReceivedWhenRequestIsAccepted()
        {
            var returnRequest = ValidReturnRequest(ReturnRequestStatus.Accepted);
            _returnRequests.Setup(rr => rr.FindAsync(It.IsAny<Expression<Func<ReturnRequest, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(returnRequest);

            var result = await _receiveReturnRequestHandler.Handle(new ReceiveReturnRequestCommand(returnRequest.Id), It.IsAny<CancellationToken>());

            Assert.That(result.IsSuccess, Is.True);
            _returnRequests.Verify(rr => rr.Update(It.Is<ReturnRequest>(req =>
                req.Status == ReturnRequestStatus.Received &&
                req.ReceivedAt != null)), Times.Once);
            _notificationService.Verify(n => n.NotifyUserAsync(_userId, It.IsAny<string>(), _userId, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task EnsureReceiveReturnRequestHandlerReturnsFailureWhenRequestIsNotAccepted()
        {
            var returnRequest = ValidReturnRequest(ReturnRequestStatus.Pending);
            _returnRequests.Setup(rr => rr.FindAsync(It.IsAny<Expression<Func<ReturnRequest, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(returnRequest);

            var result = await _receiveReturnRequestHandler.Handle(new ReceiveReturnRequestCommand(returnRequest.Id), It.IsAny<CancellationToken>());

            Assert.That(result.IsSuccess, Is.False);
            _returnRequests.Verify(rr => rr.Update(It.IsAny<ReturnRequest>()), Times.Never);
        }

        [Test]
        public void EnsureReceiveReturnRequestHandlerThrowsNotFoundWhenRequestNotFound()
        {
            _returnRequests.Setup(rr => rr.FindAsync(It.IsAny<Expression<Func<ReturnRequest, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ReturnRequest?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _receiveReturnRequestHandler.Handle(new ReceiveReturnRequestCommand(Guid.NewGuid()), It.IsAny<CancellationToken>()));
        }

        #endregion

        #region RejectReturnRequest

        [Test]
        public async Task EnsureRejectReturnRequestHandlerRejectsPendingRequest()
        {
            var returnRequest = ValidReturnRequest();
            _returnRequests.Setup(rr => rr.FindAsync(It.IsAny<Expression<Func<ReturnRequest, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(returnRequest);

            var result = await _rejectReturnRequestHandler.Handle(new RejectReturnRequestCommand(returnRequest.Id, "Item shows usage"), It.IsAny<CancellationToken>());

            Assert.That(result.IsSuccess, Is.True);
            _returnRequests.Verify(rr => rr.Update(It.Is<ReturnRequest>(req =>
                req.Status == ReturnRequestStatus.Rejected &&
                req.AdminNote == "Item shows usage")), Times.Once);
            _notificationService.Verify(n => n.NotifyUserAsync(_userId, It.IsAny<string>(), _userId, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task EnsureRejectReturnRequestHandlerReturnsFailureWhenRequestCanNoLongerBeRejected()
        {
            var returnRequest = ValidReturnRequest(ReturnRequestStatus.Completed);
            _returnRequests.Setup(rr => rr.FindAsync(It.IsAny<Expression<Func<ReturnRequest, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(returnRequest);

            var result = await _rejectReturnRequestHandler.Handle(new RejectReturnRequestCommand(returnRequest.Id, "Too late"), It.IsAny<CancellationToken>());

            Assert.That(result.IsSuccess, Is.False);
            _returnRequests.Verify(rr => rr.Update(It.IsAny<ReturnRequest>()), Times.Never);
        }

        [Test]
        public void EnsureRejectReturnRequestHandlerThrowsNotFoundWhenRequestNotFound()
        {
            _returnRequests.Setup(rr => rr.FindAsync(It.IsAny<Expression<Func<ReturnRequest, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ReturnRequest?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _rejectReturnRequestHandler.Handle(new RejectReturnRequestCommand(Guid.NewGuid(), "No"), It.IsAny<CancellationToken>()));
        }

        #endregion

        #region ProcessReturnRefund

        [Test]
        public async Task EnsureProcessReturnRefundHandlerRefundsItemsOnlyWhenReasonIsCustomerChoice()
        {
            var returnRequest = ValidReturnRequest(ReturnRequestStatus.Received);
            var orderItemId = Guid.NewGuid();
            returnRequest.Order = new Order { OrderId = returnRequest.OrderId, ShippingFee = 50m };
            returnRequest.ReturnItems = new List<ReturnItem> { new() { OrderItemId = orderItemId, ProductName = "Sofa", Quantity = 2 } };
            _returnRequests.Setup(rr => rr.FindAsync(It.IsAny<Expression<Func<ReturnRequest, bool>>>(), It.IsAny<Expression<Func<ReturnRequest, object>>[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(returnRequest);
            _orderItems.Setup(oi => oi.FindAsync(It.IsAny<Expression<Func<OrderItem, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OrderItem { OrderItemId = orderItemId, UnitPrice = 100m, Quantity = 2 });
            _sender.Setup(s => s.Send(It.IsAny<RefundPaymentCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResponseResult<App.Application.Payments.Dto.PaymentOperationResultDto>.Success(new App.Application.Payments.Dto.PaymentOperationResultDto()));

            var result = await _processReturnRefundHandler.Handle(new ProcessReturnRefundCommand(returnRequest.Id, null), It.IsAny<CancellationToken>());

            Assert.That(result.IsSuccess, Is.True);
            _sender.Verify(s => s.Send(It.Is<RefundPaymentCommand>(cmd => cmd.OrderId == returnRequest.OrderId && cmd.AmountCents == 20000), It.IsAny<CancellationToken>()), Times.Once);
            _returnRequests.Verify(rr => rr.Update(It.Is<ReturnRequest>(req =>
                req.Status == ReturnRequestStatus.Completed &&
                req.RefundAmountCents == 20000)), Times.Once);
            _notificationService.Verify(n => n.NotifyUserAsync(_userId, It.IsAny<string>(), _userId, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task EnsureProcessReturnRefundHandlerIncludesShippingWhenReasonIsMerchantDefect()
        {
            var returnRequest = ValidReturnRequest(ReturnRequestStatus.Received);
            returnRequest.ReasonCategory = ReturnReasonCategory.MerchantDefect;
            var orderItemId = Guid.NewGuid();
            returnRequest.Order = new Order { OrderId = returnRequest.OrderId, ShippingFee = 50m };
            returnRequest.ReturnItems = new List<ReturnItem> { new() { OrderItemId = orderItemId, ProductName = "Sofa", Quantity = 2 } };
            _returnRequests.Setup(rr => rr.FindAsync(It.IsAny<Expression<Func<ReturnRequest, bool>>>(), It.IsAny<Expression<Func<ReturnRequest, object>>[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(returnRequest);
            _orderItems.Setup(oi => oi.FindAsync(It.IsAny<Expression<Func<OrderItem, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OrderItem { OrderItemId = orderItemId, UnitPrice = 100m, Quantity = 2 });
            _sender.Setup(s => s.Send(It.IsAny<RefundPaymentCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResponseResult<App.Application.Payments.Dto.PaymentOperationResultDto>.Success(new App.Application.Payments.Dto.PaymentOperationResultDto()));

            var result = await _processReturnRefundHandler.Handle(new ProcessReturnRefundCommand(returnRequest.Id, null), It.IsAny<CancellationToken>());

            Assert.That(result.IsSuccess, Is.True);
            _sender.Verify(s => s.Send(It.Is<RefundPaymentCommand>(cmd => cmd.AmountCents == 25000), It.IsAny<CancellationToken>()), Times.Once);
            _returnRequests.Verify(rr => rr.Update(It.Is<ReturnRequest>(req => req.RefundAmountCents == 25000)), Times.Once);
        }

        [Test]
        public async Task EnsureProcessReturnRefundHandlerReturnsFailureWhenAmountExceedsCap()
        {
            var returnRequest = ValidReturnRequest(ReturnRequestStatus.Received);
            var orderItemId = Guid.NewGuid();
            returnRequest.Order = new Order { OrderId = returnRequest.OrderId, ShippingFee = 50m };
            returnRequest.ReturnItems = new List<ReturnItem> { new() { OrderItemId = orderItemId, ProductName = "Sofa", Quantity = 1 } };
            _returnRequests.Setup(rr => rr.FindAsync(It.IsAny<Expression<Func<ReturnRequest, bool>>>(), It.IsAny<Expression<Func<ReturnRequest, object>>[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(returnRequest);
            _orderItems.Setup(oi => oi.FindAsync(It.IsAny<Expression<Func<OrderItem, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OrderItem { OrderItemId = orderItemId, UnitPrice = 100m, Quantity = 1 });

            var result = await _processReturnRefundHandler.Handle(new ProcessReturnRefundCommand(returnRequest.Id, 999999), It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Errors[0], Does.Contain("Refund amount must be between 1 and 10000 cents"));
            });
            _sender.Verify(s => s.Send(It.IsAny<RefundPaymentCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task EnsureProcessReturnRefundHandlerReturnsFailureWhenRefundPaymentFails()
        {
            var returnRequest = ValidReturnRequest(ReturnRequestStatus.Received);
            var orderItemId = Guid.NewGuid();
            returnRequest.Order = new Order { OrderId = returnRequest.OrderId, ShippingFee = 50m };
            returnRequest.ReturnItems = new List<ReturnItem> { new() { OrderItemId = orderItemId, ProductName = "Sofa", Quantity = 1 } };
            _returnRequests.Setup(rr => rr.FindAsync(It.IsAny<Expression<Func<ReturnRequest, bool>>>(), It.IsAny<Expression<Func<ReturnRequest, object>>[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(returnRequest);
            _orderItems.Setup(oi => oi.FindAsync(It.IsAny<Expression<Func<OrderItem, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OrderItem { OrderItemId = orderItemId, UnitPrice = 100m, Quantity = 1 });
            _sender.Setup(s => s.Send(It.IsAny<RefundPaymentCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResponseResult<App.Application.Payments.Dto.PaymentOperationResultDto>.Failure("Paymob rejected the refund"));

            var result = await _processReturnRefundHandler.Handle(new ProcessReturnRefundCommand(returnRequest.Id, null), It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Errors, Has.Some.EqualTo("Paymob rejected the refund"));
            });
            _returnRequests.Verify(rr => rr.Update(It.IsAny<ReturnRequest>()), Times.Never);
        }

        [Test]
        public async Task EnsureProcessReturnRefundHandlerReturnsFailureWhenRequestIsNotReceived()
        {
            var returnRequest = ValidReturnRequest(ReturnRequestStatus.Pending);
            _returnRequests.Setup(rr => rr.FindAsync(It.IsAny<Expression<Func<ReturnRequest, bool>>>(), It.IsAny<Expression<Func<ReturnRequest, object>>[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(returnRequest);

            var result = await _processReturnRefundHandler.Handle(new ProcessReturnRefundCommand(returnRequest.Id, null), It.IsAny<CancellationToken>());

            Assert.That(result.IsSuccess, Is.False);
            _sender.Verify(s => s.Send(It.IsAny<RefundPaymentCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public void EnsureProcessReturnRefundHandlerThrowsNotFoundWhenRequestNotFound()
        {
            _returnRequests.Setup(rr => rr.FindAsync(It.IsAny<Expression<Func<ReturnRequest, bool>>>(), It.IsAny<Expression<Func<ReturnRequest, object>>[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ReturnRequest?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _processReturnRefundHandler.Handle(new ProcessReturnRefundCommand(Guid.NewGuid(), null), It.IsAny<CancellationToken>()));
        }

        #endregion

        #region GetReturnRequests

        [Test]
        public async Task EnsureGetReturnRequestsHandlerReturnsMappedRequestsWithItems()
        {
            var returnRequest = ValidReturnRequest();
            returnRequest.Customer = new AppUser { Id = _userId, FName = "Ahmed", LName = "Ali" };
            returnRequest.CourierName = "Aramex";
            returnRequest.ReturnItems = new List<ReturnItem> { new() { OrderItemId = Guid.NewGuid(), ProductName = "Sofa", Quantity = 2 } };
            _returnRequests.Setup(rr => rr.FindPagedAsync(
                    It.IsAny<Expression<Func<ReturnRequest, bool>>>(),
                    It.IsAny<Expression<Func<ReturnRequest, object>>[]>(),
                    It.IsAny<App.Application.Common.Enums.SortDirection>(),
                    It.IsAny<Expression<Func<ReturnRequest, object>>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(((IReadOnlyList<ReturnRequest>)[returnRequest], 1));

            var result = await _getReturnRequestsHandler.Handle(new GetReturnRequestsQuery(null), It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.True);
                Assert.That(result.Value!.TotalCount, Is.EqualTo(1));
                Assert.That(result.Value.Items, Has.Count.EqualTo(1));
                Assert.That(result.Value.Items[0].CustomerName, Is.EqualTo("Ahmed Ali"));
                Assert.That(result.Value.Items[0].ReasonCategory, Is.EqualTo("CustomerChoice"));
                Assert.That(result.Value.Items[0].Status, Is.EqualTo(ReturnRequestStatus.Pending));
                Assert.That(result.Value.Items[0].CourierName, Is.EqualTo("Aramex"));
                Assert.That(result.Value.Items[0].Items, Has.Count.EqualTo(1));
                Assert.That(result.Value.Items[0].Items[0].ProductName, Is.EqualTo("Sofa"));
                Assert.That(result.Value.Items[0].Items[0].Quantity, Is.EqualTo(2));
            });
        }

        #endregion
    }
}
