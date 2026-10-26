using System.Linq.Expressions;
using System.Text.Json;
using App.Application.Common.Enums;
using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Repos;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Payments.Command.CreatePaymentSession;
using App.Application.Payments.Command.HandlePaymobCallback;
using App.Application.Payments.Command.RefundPayment;
using App.Application.Payments.Command.VoidPayment;
using App.Application.Payments.Dto;
using App.Application.Payments.Dto.Paymob;
using App.Application.Payments.Queries.GetPaymentTransactions;
using App.Application.Responses;
using App.Domain.Entities;
using App.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace App.Test.Handlers
{
    [TestFixture]
    [Category("Payments")]
    public class PaymentsHandlerTest
    {
        private Mock<IUnitOfWork> _unitOfWork = null!;
        private Mock<IBaseRepository<Order>> _orders = null!;
        private Mock<IBaseRepository<Product>> _products = null!;
        private Mock<IBaseRepository<Payment>> _payments = null!;
        private Mock<IBaseRepository<PaymentTransaction>> _paymentTransactions = null!;
        private Mock<IBaseRepository<IdempotencyRecord>> _idempotencyRecords = null!;
        private Mock<ICurrentUserService> _currentUserService = null!;
        private Mock<IPaymobService> _paymobService = null!;

        private CreatePaymentSessionHandler _createPaymentSessionHandler = null!;
        private HandlePaymobCallbackHandler _handlePaymobCallbackHandler = null!;
        private GetPaymentTransactionsHandler _getPaymentTransactionsHandler = null!;
        private RefundPaymentHandler _refundPaymentHandler = null!;
        private VoidPaymentHandler _voidPaymentHandler = null!;

        private readonly Guid _userId = Guid.NewGuid();

        [SetUp]
        public void Setup()
        {
            _unitOfWork = new Mock<IUnitOfWork>();
            _orders = new Mock<IBaseRepository<Order>>();
            _products = new Mock<IBaseRepository<Product>>();
            _payments = new Mock<IBaseRepository<Payment>>();
            _paymentTransactions = new Mock<IBaseRepository<PaymentTransaction>>();
            _idempotencyRecords = new Mock<IBaseRepository<IdempotencyRecord>>();
            _currentUserService = new Mock<ICurrentUserService>();
            _paymobService = new Mock<IPaymobService>();

            _unitOfWork.Setup(u => u.Orders).Returns(_orders.Object);
            _unitOfWork.Setup(u => u.Products).Returns(_products.Object);
            _unitOfWork.Setup(u => u.Payments).Returns(_payments.Object);
            _unitOfWork.Setup(u => u.PaymentTransactions).Returns(_paymentTransactions.Object);
            _unitOfWork.Setup(u => u.IdempotencyRecords).Returns(_idempotencyRecords.Object);

            _currentUserService.SetupGet(c => c.UserId).Returns(_userId);

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Paymob:IntegrationsId"] = "1,2",
                    ["Paymob:NotificationUrl"] = "https://furni.com/api/Payment/callback",
                    ["Paymob:RedirectionUrl"] = "https://furni.com/payment-result"
                })
                .Build();

            _createPaymentSessionHandler = new CreatePaymentSessionHandler(_unitOfWork.Object, _paymobService.Object, _currentUserService.Object, configuration, new Mock<ILogger<CreatePaymentSessionHandler>>().Object);
            _handlePaymobCallbackHandler = new HandlePaymobCallbackHandler(_unitOfWork.Object, _paymobService.Object, new Mock<ILogger<HandlePaymobCallbackHandler>>().Object);
            _getPaymentTransactionsHandler = new GetPaymentTransactionsHandler(_unitOfWork.Object, new Mock<ILogger<GetPaymentTransactionsHandler>>().Object);
            _refundPaymentHandler = new RefundPaymentHandler(_unitOfWork.Object, _paymobService.Object, new Mock<ILogger<RefundPaymentHandler>>().Object);
            _voidPaymentHandler = new VoidPaymentHandler(_unitOfWork.Object, _paymobService.Object, new Mock<ILogger<VoidPaymentHandler>>().Object);
        }

        private Order ValidPaymentOrder() => new()
        {
            OrderId = Guid.NewGuid(),
            UserId = _userId,
            TotalPrice = 250m,
            ShippingFee = 50m,
            OrderStatus = OrderStatus.Processing,
            Customer = new AppUser { Id = _userId, FName = "Ahmed", LName = "Ali", Email = "ahmedali@gmail.com", PhoneNumber = "01012345678", Address = "12 Tahrir Square", City = "Cairo", Country = "Egypt" },
            OrderItems = new List<OrderItem> { new() { OrderItemId = Guid.NewGuid(), ProductId = Guid.NewGuid(), Quantity = 2, UnitPrice = 100m } }
        };

        private void SetupValidPaymentOrder(Order order)
        {
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<Expression<Func<Order, object>>[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);
            _products.Setup(p => p.FindAllAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Product> { new() { ProductId = order.OrderItems.First().ProductId, ProductName = "Sofa" } });
            _payments.Setup(p => p.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Payment?)null);
        }

        #region CreatePaymentSession

        [Test]
        public async Task EnsureCreatePaymentSessionHandlerReturnsSessionWhenOrderIsValid()
        {
            var order = ValidPaymentOrder();
            SetupValidPaymentOrder(order);
            _paymobService.Setup(p => p.CreateIntentionAsync(It.IsAny<PaymobIntentionRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PaymobIntentionResponse { Id = "int-1", ClientSecret = "secret-1" });
            _paymobService.Setup(p => p.BuildCheckoutUrl("secret-1")).Returns("https://checkout.furni.com/pay");

            var result = await _createPaymentSessionHandler.Handle(new CreatePaymentSessionCommand { OrderId = order.OrderId }, It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.True);
                Assert.That(result.Value.IntentionId, Is.EqualTo("int-1"));
                Assert.That(result.Value.ClientSecret, Is.EqualTo("secret-1"));
                Assert.That(result.Value.CheckoutUrl, Is.EqualTo("https://checkout.furni.com/pay"));
            });
            _paymobService.Verify(p => p.CreateIntentionAsync(It.Is<PaymobIntentionRequest>(r =>
                r.AmountCents == 25000 &&
                r.Currency == "EGP" &&
                r.PaymentMethods.Count == 2 &&
                r.SpecialReference == order.OrderId.ToString() &&
                r.Expiration == 3600 &&
                r.Items.Count == 2 &&
                r.Items[0].Name == "Sofa" &&
                r.Items[0].AmountCents == 10000 &&
                r.Items[1].Name == "Shipping" &&
                r.Items[1].AmountCents == 5000), It.IsAny<CancellationToken>()), Times.Once);
            _idempotencyRecords.Verify(r => r.AddAsync(It.IsAny<IdempotencyRecord>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task EnsureCreatePaymentSessionHandlerStoresIdempotencyRecordWhenKeyIsProvided()
        {
            var order = ValidPaymentOrder();
            SetupValidPaymentOrder(order);
            _idempotencyRecords.Setup(r => r.FindAsync(It.IsAny<Expression<Func<IdempotencyRecord, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IdempotencyRecord?)null);
            _paymobService.Setup(p => p.CreateIntentionAsync(It.IsAny<PaymobIntentionRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PaymobIntentionResponse { Id = "int-1", ClientSecret = "secret-1" });
            _paymobService.Setup(p => p.BuildCheckoutUrl(It.IsAny<string>())).Returns("https://checkout.furni.com/pay");

            var result = await _createPaymentSessionHandler.Handle(new CreatePaymentSessionCommand { OrderId = order.OrderId, IdempotencyKey = "key-1" }, It.IsAny<CancellationToken>());

            Assert.That(result.IsSuccess, Is.True);
            _idempotencyRecords.Verify(r => r.AddAsync(It.Is<IdempotencyRecord>(rec =>
                rec.UserId == _userId &&
                rec.IdempotencyKey == "key-1" &&
                rec.OrderId == order.OrderId &&
                !string.IsNullOrWhiteSpace(rec.ResponsePayload)), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task EnsureCreatePaymentSessionHandlerReturnsStoredSessionWhenSameKeyIsReplayed()
        {
            var order = ValidPaymentOrder();
            SetupValidPaymentOrder(order);
            var stored = new PaymentSessionDto { IntentionId = "int-1", ClientSecret = "secret-1", CheckoutUrl = "https://checkout.furni.com/pay" };
            _idempotencyRecords.Setup(r => r.FindAsync(It.IsAny<Expression<Func<IdempotencyRecord, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new IdempotencyRecord { UserId = _userId, IdempotencyKey = "key-1", OrderId = order.OrderId, ResponsePayload = JsonSerializer.Serialize(stored), UpdatedAt = DateTime.UtcNow });

            var result = await _createPaymentSessionHandler.Handle(new CreatePaymentSessionCommand { OrderId = order.OrderId, IdempotencyKey = "key-1" }, It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.True);
                Assert.That(result.Value.IntentionId, Is.EqualTo("int-1"));
                Assert.That(result.Value.CheckoutUrl, Is.EqualTo("https://checkout.furni.com/pay"));
            });
            _paymobService.Verify(p => p.CreateIntentionAsync(It.IsAny<PaymobIntentionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task EnsureCreatePaymentSessionHandlerReturnsFailureWhenKeyWasUsedForDifferentOrder()
        {
            var order = ValidPaymentOrder();
            SetupValidPaymentOrder(order);
            _idempotencyRecords.Setup(r => r.FindAsync(It.IsAny<Expression<Func<IdempotencyRecord, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new IdempotencyRecord { UserId = _userId, IdempotencyKey = "key-1", OrderId = Guid.NewGuid(), ResponsePayload = "{}", UpdatedAt = DateTime.UtcNow });

            var result = await _createPaymentSessionHandler.Handle(new CreatePaymentSessionCommand { OrderId = order.OrderId, IdempotencyKey = "key-1" }, It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Errors, Has.Some.EqualTo("This idempotency key was already used for a different order."));
            });
        }

        [Test]
        public async Task EnsureCreatePaymentSessionHandlerReturnsFailureWhenOrderIsAlreadyPaid()
        {
            var order = ValidPaymentOrder();
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<Expression<Func<Order, object>>[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);
            _payments.Setup(p => p.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Payment { OrderId = order.OrderId, PaymentStatus = "Completed" });

            var result = await _createPaymentSessionHandler.Handle(new CreatePaymentSessionCommand { OrderId = order.OrderId }, It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Errors, Has.Some.EqualTo("This order has already been paid."));
            });
            _paymobService.Verify(p => p.CreateIntentionAsync(It.IsAny<PaymobIntentionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public void EnsureCreatePaymentSessionHandlerThrowsForbiddenWhenOrderBelongsToAnotherUser()
        {
            var order = ValidPaymentOrder();
            order.UserId = Guid.NewGuid();
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<Expression<Func<Order, object>>[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            Assert.ThrowsAsync<ForbiddenException>(async () =>
                await _createPaymentSessionHandler.Handle(new CreatePaymentSessionCommand { OrderId = order.OrderId }, It.IsAny<CancellationToken>()));
        }

        [Test]
        public void EnsureCreatePaymentSessionHandlerThrowsNotFoundWhenOrderNotFound()
        {
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<Expression<Func<Order, object>>[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Order?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _createPaymentSessionHandler.Handle(new CreatePaymentSessionCommand { OrderId = Guid.NewGuid() }, It.IsAny<CancellationToken>()));
        }

        [Test]
        public void EnsureCreatePaymentSessionHandlerThrowsNotFoundWhenUserIsNull()
        {
            _currentUserService.SetupGet(c => c.UserId).Returns((Guid?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _createPaymentSessionHandler.Handle(new CreatePaymentSessionCommand { OrderId = Guid.NewGuid() }, It.IsAny<CancellationToken>()));
        }

        #endregion

        #region HandlePaymobCallback

        private PaymobCallbackDto ValidCallback(Guid orderId) => new()
        {
            Obj = new PaymobTransactionDto
            {
                Id = 123456,
                Success = true,
                AmountCents = 35000,
                Currency = "EGP",
                Order = new PaymobOrderDto { Id = 55, MerchantOrderId = orderId.ToString() },
                SourceData = new PaymobSourceDataDto { Type = "card", SubType = "Visa" },
                CreatedAt = "2026-01-01T10:00:00"
            }
        };

        [Test]
        public async Task EnsureHandlePaymobCallbackHandlerRecordsTransactionWhenHmacIsValid()
        {
            var order = ValidPaymentOrder();
            var body = ValidCallback(order.OrderId);
            _paymobService.Setup(p => p.VerifyTransactionHmac(It.IsAny<PaymobTransactionDto>(), It.IsAny<string>()))
                .Returns(true);
            _paymentTransactions.Setup(pt => pt.FindAsync(It.IsAny<Expression<Func<PaymentTransaction, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((PaymentTransaction?)null);
            _payments.Setup(p => p.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Payment { OrderId = order.OrderId, PaymentStatus = "Pending" });

            var result = await _handlePaymobCallbackHandler.Handle(new HandlePaymobCallbackCommand(body, "valid-hmac"), It.IsAny<CancellationToken>());

            Assert.That(result, Is.True);
            _paymentTransactions.Verify(pt => pt.AddAsync(It.Is<PaymentTransaction>(t =>
                t.PaymobTransactionId == 123456 &&
                t.PaymobOrderId == 55 &&
                t.OrderId == order.OrderId &&
                t.Amount == 350m &&
                t.Status == "Success" &&
                t.PaymentMethod == "Visa" &&
                t.SourceType == "card"), It.IsAny<CancellationToken>()), Times.Once);
            _payments.Verify(p => p.Update(It.Is<Payment>(pay =>
                pay.PaymentStatus == "Completed" &&
                pay.PaymentMethod == "Visa")), Times.Once);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task EnsureHandlePaymobCallbackHandlerMapsPendingStatusWhenTransactionIsPending()
        {
            var order = ValidPaymentOrder();
            var body = ValidCallback(order.OrderId);
            body = body with { Obj = body.Obj! with { Success = false, Pending = true } };
            _paymobService.Setup(p => p.VerifyTransactionHmac(It.IsAny<PaymobTransactionDto>(), It.IsAny<string>()))
                .Returns(true);
            _paymentTransactions.Setup(pt => pt.FindAsync(It.IsAny<Expression<Func<PaymentTransaction, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((PaymentTransaction?)null);
            _payments.Setup(p => p.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Payment { OrderId = order.OrderId, PaymentStatus = "Pending" });

            var result = await _handlePaymobCallbackHandler.Handle(new HandlePaymobCallbackCommand(body, "valid-hmac"), It.IsAny<CancellationToken>());

            Assert.That(result, Is.True);
            _paymentTransactions.Verify(pt => pt.AddAsync(It.Is<PaymentTransaction>(t => t.Status == "Pending"), It.IsAny<CancellationToken>()), Times.Once);
            _payments.Verify(p => p.Update(It.Is<Payment>(pay => pay.PaymentStatus == "Pending")), Times.Once);
        }

        [Test]
        public async Task EnsureHandlePaymobCallbackHandlerReturnsFalseWhenBodyHasNoTransaction()
        {
            var result = await _handlePaymobCallbackHandler.Handle(new HandlePaymobCallbackCommand(new PaymobCallbackDto(), "hmac"), It.IsAny<CancellationToken>());

            Assert.That(result, Is.False);
            _paymentTransactions.Verify(pt => pt.AddAsync(It.IsAny<PaymentTransaction>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task EnsureHandlePaymobCallbackHandlerReturnsFalseWhenHmacIsInvalid()
        {
            var body = ValidCallback(Guid.NewGuid());
            _paymobService.Setup(p => p.VerifyTransactionHmac(It.IsAny<PaymobTransactionDto>(), It.IsAny<string>()))
                .Returns(false);

            var result = await _handlePaymobCallbackHandler.Handle(new HandlePaymobCallbackCommand(body, "bad-hmac"), It.IsAny<CancellationToken>());

            Assert.That(result, Is.False);
            _paymentTransactions.Verify(pt => pt.AddAsync(It.IsAny<PaymentTransaction>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task EnsureHandlePaymobCallbackHandlerReturnsFalseWhenTransactionIsAlreadyRecorded()
        {
            var body = ValidCallback(Guid.NewGuid());
            _paymobService.Setup(p => p.VerifyTransactionHmac(It.IsAny<PaymobTransactionDto>(), It.IsAny<string>()))
                .Returns(true);
            _paymentTransactions.Setup(pt => pt.FindAsync(It.IsAny<Expression<Func<PaymentTransaction, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PaymentTransaction { PaymobTransactionId = 123456 });

            var result = await _handlePaymobCallbackHandler.Handle(new HandlePaymobCallbackCommand(body, "valid-hmac"), It.IsAny<CancellationToken>());

            Assert.That(result, Is.False);
            _paymentTransactions.Verify(pt => pt.AddAsync(It.IsAny<PaymentTransaction>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task EnsureHandlePaymobCallbackHandlerSkipsPaymentUpdateWhenOrderCannotBeMatched()
        {
            var body = ValidCallback(Guid.NewGuid());
            body = body with { Obj = body.Obj! with { Order = new PaymobOrderDto { Id = 55, MerchantOrderId = "not-a-guid" } } };
            _paymobService.Setup(p => p.VerifyTransactionHmac(It.IsAny<PaymobTransactionDto>(), It.IsAny<string>()))
                .Returns(true);
            _paymentTransactions.Setup(pt => pt.FindAsync(It.IsAny<Expression<Func<PaymentTransaction, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((PaymentTransaction?)null);

            var result = await _handlePaymobCallbackHandler.Handle(new HandlePaymobCallbackCommand(body, "valid-hmac"), It.IsAny<CancellationToken>());

            Assert.That(result, Is.True);
            _paymentTransactions.Verify(pt => pt.AddAsync(It.Is<PaymentTransaction>(t => t.OrderId == null), It.IsAny<CancellationToken>()), Times.Once);
            _payments.Verify(p => p.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region GetPaymentTransactions

        [Test]
        public async Task EnsureGetPaymentTransactionsHandlerReturnsPagedTransactions()
        {
            var transaction = new PaymentTransaction
            {
                Id = Guid.NewGuid(),
                PaymobTransactionId = 123456,
                PaymobOrderId = 55,
                OrderId = Guid.NewGuid(),
                Amount = 350m,
                Currency = "EGP",
                Status = "Success",
                PaymentMethod = "Visa",
                SourceType = "card",
                Order = new Order { OrderId = Guid.NewGuid(), Customer = new AppUser { Id = _userId, FName = "Ahmed", LName = "Ali" } }
            };
            _paymentTransactions.Setup(pt => pt.FindPagedAsync(
                It.IsAny<Expression<Func<PaymentTransaction, bool>>>(),
                It.IsAny<Expression<Func<PaymentTransaction, object>>[]>(),
                It.IsAny<SortDirection>(),
                It.IsAny<Expression<Func<PaymentTransaction, object>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(((IReadOnlyList<PaymentTransaction>)[transaction], 1));

            var result = await _getPaymentTransactionsHandler.Handle(new GetPaymentTransactionsQuery { Page = 2, PageSize = 10 }, It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.Page, Is.EqualTo(2));
                Assert.That(result.PageSize, Is.EqualTo(10));
                Assert.That(result.TotalCount, Is.EqualTo(1));
                Assert.That(result.Items, Has.Count.EqualTo(1));
                Assert.That(result.Items[0].PaymobTransactionId, Is.EqualTo(123456));
                Assert.That(result.Items[0].CustomerName, Is.EqualTo("Ahmed Ali"));
                Assert.That(result.Items[0].Amount, Is.EqualTo(350m));
                Assert.That(result.Items[0].Status, Is.EqualTo("Success"));
            });
        }

        #endregion

        #region RefundPayment

        private Order DeliveredPaidOrder() 
        {
            var order = ValidPaymentOrder();
            order.OrderStatus = OrderStatus.Delivered;
            order.UpdatedAt = DateTime.UtcNow;
            return order;
        }

        private void SetupRefundableTransaction(Order order)
        {
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);
            _paymentTransactions.Setup(pt => pt.FindAllAsync(It.IsAny<Expression<Func<PaymentTransaction, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<PaymentTransaction>
                {
                    new() { PaymobTransactionId = 123456, OrderId = order.OrderId, Status = "Success", Amount = 100m, RefundedAmount = null, PaymobCreatedAt = DateTime.UtcNow }
                });
            _payments.Setup(p => p.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Payment { OrderId = order.OrderId, PaymentStatus = "Completed" });
        }

        [Test]
        public async Task EnsureRefundPaymentHandlerRefundsFullAmountWhenAmountIsOmitted()
        {
            var order = DeliveredPaidOrder();
            SetupRefundableTransaction(order);
            _paymobService.Setup(p => p.RefundTransactionAsync(123456, 10000, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PaymobOperationResult { Success = true, Message = "Refunded" });

            var result = await _refundPaymentHandler.Handle(new RefundPaymentCommand { OrderId = order.OrderId }, It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.True);
                Assert.That(result.Value.PaymobTransactionId, Is.EqualTo(123456));
                Assert.That(result.Value.Status, Is.EqualTo("Refunded"));
                Assert.That(result.Value.RefundedAmount, Is.EqualTo(100m));
            });
            _paymentTransactions.Verify(pt => pt.Update(It.Is<PaymentTransaction>(t =>
                t.RefundedAmount == 100m &&
                t.Status == "Refunded")), Times.Once);
            _payments.Verify(p => p.Update(It.Is<Payment>(pay => pay.PaymentStatus == "Refunded")), Times.Once);
        }

        [Test]
        public async Task EnsureRefundPaymentHandlerRefundsPartialAmountWhenSpecified()
        {
            var order = DeliveredPaidOrder();
            SetupRefundableTransaction(order);
            _paymobService.Setup(p => p.RefundTransactionAsync(123456, 5000, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PaymobOperationResult { Success = true });

            var result = await _refundPaymentHandler.Handle(new RefundPaymentCommand { OrderId = order.OrderId, AmountCents = 5000 }, It.IsAny<CancellationToken>());

            Assert.That(result.IsSuccess, Is.True);
            _paymentTransactions.Verify(pt => pt.Update(It.Is<PaymentTransaction>(t =>
                t.RefundedAmount == 50m &&
                t.Status == "Success")), Times.Once);
        }

        [Test]
        public async Task EnsureRefundPaymentHandlerReturnsFailureWhenOrderIsNotDelivered()
        {
            var order = ValidPaymentOrder();
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            var result = await _refundPaymentHandler.Handle(new RefundPaymentCommand { OrderId = order.OrderId }, It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Errors, Has.Some.EqualTo("Refunds are only allowed within 14 days after delivery."));
            });
            _paymobService.Verify(p => p.RefundTransactionAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task EnsureRefundPaymentHandlerReturnsFailureWhenNoSuccessfulTransactionExists()
        {
            var order = DeliveredPaidOrder();
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);
            _paymentTransactions.Setup(pt => pt.FindAllAsync(It.IsAny<Expression<Func<PaymentTransaction, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<PaymentTransaction>
                {
                    new() { PaymobTransactionId = 123456, OrderId = order.OrderId, Status = "Failed", Amount = 100m }
                });

            var result = await _refundPaymentHandler.Handle(new RefundPaymentCommand { OrderId = order.OrderId }, It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Errors, Has.Some.EqualTo("No successful payment found for this order."));
            });
        }

        [Test]
        public async Task EnsureRefundPaymentHandlerReturnsFailureWhenAmountExceedsRemaining()
        {
            var order = DeliveredPaidOrder();
            SetupRefundableTransaction(order);

            var result = await _refundPaymentHandler.Handle(new RefundPaymentCommand { OrderId = order.OrderId, AmountCents = 999999 }, It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Errors[0], Does.Contain("Refund amount must be between 1 and 10000 cents."));
            });
            _paymobService.Verify(p => p.RefundTransactionAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task EnsureRefundPaymentHandlerReturnsFailureWhenPaymobRejectsTheRefund()
        {
            var order = DeliveredPaidOrder();
            SetupRefundableTransaction(order);
            _paymobService.Setup(p => p.RefundTransactionAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PaymobOperationResult { Success = false, Message = "Amount not refundable" });

            var result = await _refundPaymentHandler.Handle(new RefundPaymentCommand { OrderId = order.OrderId }, It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Errors, Has.Some.EqualTo("Amount not refundable"));
            });
            _paymentTransactions.Verify(pt => pt.Update(It.IsAny<PaymentTransaction>()), Times.Never);
        }

        [Test]
        public void EnsureRefundPaymentHandlerThrowsNotFoundWhenOrderNotFound()
        {
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Order?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _refundPaymentHandler.Handle(new RefundPaymentCommand { OrderId = Guid.NewGuid() }, It.IsAny<CancellationToken>()));
        }

        #endregion

        #region VoidPayment

        private void SetupVoidableTransaction(Order order)
        {
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);
            _paymentTransactions.Setup(pt => pt.FindAllAsync(It.IsAny<Expression<Func<PaymentTransaction, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<PaymentTransaction>
                {
                    new() { PaymobTransactionId = 123456, OrderId = order.OrderId, Status = "Success", Amount = 100m, PaymobCreatedAt = DateTime.UtcNow }
                });
            _payments.Setup(p => p.FindAsync(It.IsAny<Expression<Func<Payment, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Payment { OrderId = order.OrderId, PaymentStatus = "Completed" });
        }

        [Test]
        public async Task EnsureVoidPaymentHandlerVoidsPaymentWithinWindow()
        {
            var order = ValidPaymentOrder();
            SetupVoidableTransaction(order);
            _paymobService.Setup(p => p.VoidTransactionAsync(123456, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PaymobOperationResult { Success = true });

            var result = await _voidPaymentHandler.Handle(new VoidPaymentCommand { OrderId = order.OrderId }, It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.True);
                Assert.That(result.Value.PaymobTransactionId, Is.EqualTo(123456));
                Assert.That(result.Value.Status, Is.EqualTo("Voided"));
            });
            _paymentTransactions.Verify(pt => pt.Update(It.Is<PaymentTransaction>(t => t.Status == "Voided")), Times.Once);
            _payments.Verify(p => p.Update(It.Is<Payment>(pay => pay.PaymentStatus == "Voided")), Times.Once);
        }

        [Test]
        public async Task EnsureVoidPaymentHandlerReturnsFailureWhenNoSuccessfulTransactionExists()
        {
            var order = ValidPaymentOrder();
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);
            _paymentTransactions.Setup(pt => pt.FindAllAsync(It.IsAny<Expression<Func<PaymentTransaction, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<PaymentTransaction>());

            var result = await _voidPaymentHandler.Handle(new VoidPaymentCommand { OrderId = order.OrderId }, It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Errors, Has.Some.EqualTo("No successful payment found for this order."));
            });
        }

        [Test]
        public async Task EnsureVoidPaymentHandlerReturnsFailureWhenVoidWindowExpired()
        {
            var order = ValidPaymentOrder();
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);
            _paymentTransactions.Setup(pt => pt.FindAllAsync(It.IsAny<Expression<Func<PaymentTransaction, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<PaymentTransaction>
                {
                    new() { PaymobTransactionId = 123456, OrderId = order.OrderId, Status = "Success", Amount = 100m, PaymobCreatedAt = DateTime.UtcNow.AddHours(-25) }
                });

            var result = await _voidPaymentHandler.Handle(new VoidPaymentCommand { OrderId = order.OrderId }, It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Errors, Has.Some.EqualTo("Void is only allowed within 24 hours of payment."));
            });
            _paymobService.Verify(p => p.VoidTransactionAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task EnsureVoidPaymentHandlerReturnsFailureWhenPaymobRejectsTheVoid()
        {
            var order = ValidPaymentOrder();
            SetupVoidableTransaction(order);
            _paymobService.Setup(p => p.VoidTransactionAsync(123456, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PaymobOperationResult { Success = false, Message = "Transaction settled" });

            var result = await _voidPaymentHandler.Handle(new VoidPaymentCommand { OrderId = order.OrderId }, It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Errors, Has.Some.EqualTo("Transaction settled"));
            });
            _paymentTransactions.Verify(pt => pt.Update(It.IsAny<PaymentTransaction>()), Times.Never);
        }

        [Test]
        public void EnsureVoidPaymentHandlerThrowsNotFoundWhenOrderNotFound()
        {
            _orders.Setup(o => o.FindAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Order?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _voidPaymentHandler.Handle(new VoidPaymentCommand { OrderId = Guid.NewGuid() }, It.IsAny<CancellationToken>()));
        }

        #endregion

        #region PaymobIntentionPayload

        [Test]
        public void EnsurePaymobIntentionRequestUsesAmountFieldNames()
        {
            var request = new PaymobIntentionRequest
            {
                AmountCents = 12500,
                PaymentMethods = [123],
                Items = [new PaymobIntentionItem { Name = "Sofa", AmountCents = 12000, Quantity = 1 }]
            };

            using var document = JsonDocument.Parse(JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            }));
            var root = document.RootElement;

            Assert.Multiple(() =>
            {
                Assert.That(root.GetProperty("amount").GetInt64(), Is.EqualTo(12500));
                Assert.That(root.TryGetProperty("amount_cents", out _), Is.False);
                Assert.That(root.GetProperty("items")[0].GetProperty("amount").GetInt64(), Is.EqualTo(12000));
                Assert.That(root.GetProperty("items")[0].TryGetProperty("amount_cents", out _), Is.False);
            });
        }

        #endregion
    }
}
