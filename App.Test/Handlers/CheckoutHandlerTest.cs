using System.Linq.Expressions;
using App.Application.Checkout.Command.PlaceOrder;
using App.Application.Checkout.Queries.GetCheckoutDetails;
using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Repos;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Responses;
using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace App.Test.Handlers
{
    [TestFixture]
    [Category("Checkout")]
    public class CheckoutHandlerTest
    {
        private Mock<IUnitOfWork> _unitOfWork = null!;
        private Mock<IBaseRepository<ShoppingCart>> _shoppingCarts = null!;
        private Mock<IBaseRepository<CartItem>> _cartItems = null!;
        private Mock<IBaseRepository<Order>> _orders = null!;
        private Mock<IBaseRepository<OrderItem>> _orderItems = null!;
        private Mock<IBaseRepository<Payment>> _payments = null!;
        private Mock<IBaseRepository<IdempotencyRecord>> _idempotencyRecords = null!;
        private Mock<IBaseRepository<Coupon>> _coupons = null!;
        private Mock<IBaseRepository<Product>> _products = null!;
        private Mock<ICurrentUserService> _currentUserService = null!;
        private Mock<IIdentityService> _identityService = null!;
        private Mock<IShippingRateService> _shippingRateService = null!;

        private GetCheckoutDetailsHandler _getCheckoutDetailsHandler = null!;
        private PlaceOrderHandler _placeOrderHandler = null!;

        private readonly Guid _userId = Guid.NewGuid();

        [SetUp]
        public void Setup()
        {
            _unitOfWork = new Mock<IUnitOfWork>();
            _shoppingCarts = new Mock<IBaseRepository<ShoppingCart>>();
            _cartItems = new Mock<IBaseRepository<CartItem>>();
            _orders = new Mock<IBaseRepository<Order>>();
            _orderItems = new Mock<IBaseRepository<OrderItem>>();
            _payments = new Mock<IBaseRepository<Payment>>();
            _idempotencyRecords = new Mock<IBaseRepository<IdempotencyRecord>>();
            _coupons = new Mock<IBaseRepository<Coupon>>();
            _products = new Mock<IBaseRepository<Product>>();
            _currentUserService = new Mock<ICurrentUserService>();
            _identityService = new Mock<IIdentityService>();
            _shippingRateService = new Mock<IShippingRateService>();

            _unitOfWork.Setup(u => u.ShoppingCarts).Returns(_shoppingCarts.Object);
            _unitOfWork.Setup(u => u.CartItems).Returns(_cartItems.Object);
            _unitOfWork.Setup(u => u.Orders).Returns(_orders.Object);
            _unitOfWork.Setup(u => u.OrderItems).Returns(_orderItems.Object);
            _unitOfWork.Setup(u => u.Payments).Returns(_payments.Object);
            _unitOfWork.Setup(u => u.IdempotencyRecords).Returns(_idempotencyRecords.Object);
            _unitOfWork.Setup(u => u.Coupons).Returns(_coupons.Object);
            _unitOfWork.Setup(u => u.Products).Returns(_products.Object);

            _currentUserService.SetupGet(c => c.UserId).Returns(_userId);

            _getCheckoutDetailsHandler = new GetCheckoutDetailsHandler(_unitOfWork.Object, _currentUserService.Object, _identityService.Object, new Mock<ILogger<GetCheckoutDetailsHandler>>().Object);
            _placeOrderHandler = new PlaceOrderHandler(_unitOfWork.Object, _currentUserService.Object, _identityService.Object, _shippingRateService.Object, new Mock<ILogger<PlaceOrderHandler>>().Object);
        }

        private AppUser ValidUser() => new()
        {
            Id = _userId,
            FName = "Ahmed",
            LName = "Ali",
            Email = "ahmedali@gmail.com",
            Address = "12 Tahrir Square",
            City = "Cairo",
            Country = "Egypt"
        };

        [Test]
        public async Task EnsureGetCheckoutDetailsHandlerReturnsCheckoutDtoWhenCartHasItems()
        {
            var user = ValidUser();
            _identityService.Setup(i => i.GetUserByIdAsync(_userId)).ReturnsAsync(user);

            var cartId = Guid.NewGuid();
            _shoppingCarts.Setup(s => s.FindAsync(It.IsAny<Expression<Func<ShoppingCart, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ShoppingCart { Id = cartId, UserId = _userId });

            _cartItems.Setup(c => c.FindAllAsync(It.IsAny<Expression<Func<CartItem, bool>>>(), It.IsAny<Expression<Func<CartItem, object>>[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<CartItem>
                {
                    new() { ProductId = Guid.NewGuid(), ShoppingCartId = cartId, ItemQuantity = 2, Product = new Product { ProductName = "Sofa", Price = 150m, ImageUrl = "sofa.jpg" } }
                });

            var result = await _getCheckoutDetailsHandler.Handle(new GetCheckoutDetailsQuery(), It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.ShoppingCartId, Is.EqualTo(cartId));
                Assert.That(result.CustomerName, Is.EqualTo("Ahmed Ali"));
                Assert.That(result.Address, Is.EqualTo("12 Tahrir Square"));
                Assert.That(result.City, Is.EqualTo("Cairo"));
                Assert.That(result.Country, Is.EqualTo("Egypt"));
                Assert.That(result.CartItems, Has.Count.EqualTo(1));
                Assert.That(result.CartItems[0].TotalPrice, Is.EqualTo(300m));
                Assert.That(result.TotalPrice, Is.EqualTo(300m));
            });
        }

        [Test]
        public void EnsureGetCheckoutDetailsHandlerThrowsNotFoundWhenUserIsNull()
        {
            _currentUserService.SetupGet(c => c.UserId).Returns((Guid?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _getCheckoutDetailsHandler.Handle(new GetCheckoutDetailsQuery(), It.IsAny<CancellationToken>()));
        }

        [Test]
        public void EnsureGetCheckoutDetailsHandlerThrowsNotFoundWhenUserNotFound()
        {
            _identityService.Setup(i => i.GetUserByIdAsync(_userId)).ReturnsAsync((AppUser?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _getCheckoutDetailsHandler.Handle(new GetCheckoutDetailsQuery(), It.IsAny<CancellationToken>()));
        }

        [Test]
        public void EnsureGetCheckoutDetailsHandlerThrowsNotFoundWhenShoppingCartNotFound()
        {
            _identityService.Setup(i => i.GetUserByIdAsync(_userId)).ReturnsAsync(ValidUser());
            _shoppingCarts.Setup(s => s.FindAsync(It.IsAny<Expression<Func<ShoppingCart, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ShoppingCart?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _getCheckoutDetailsHandler.Handle(new GetCheckoutDetailsQuery(), It.IsAny<CancellationToken>()));
        }

        private void SetupValidCart(Product product, int quantity)
        {
            _identityService.Setup(i => i.GetUserByIdAsync(_userId)).ReturnsAsync(ValidUser());

            var cartId = Guid.NewGuid();
            _shoppingCarts.Setup(s => s.FindAsync(It.IsAny<Expression<Func<ShoppingCart, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ShoppingCart { Id = cartId, UserId = _userId });

            _cartItems.Setup(c => c.FindAllAsync(It.IsAny<Expression<Func<CartItem, bool>>>(), It.IsAny<Expression<Func<CartItem, object>>[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<CartItem>
                {
                    new() { ProductId = product.ProductId, ShoppingCartId = cartId, ItemQuantity = quantity, Product = product }
                });
        }

        [Test]
        public async Task EnsurePlaceOrderHandlerPlacesOrderWhenCartIsValid()
        {
            var product = new Product { ProductId = Guid.NewGuid(), ProductName = "Sofa", Price = 150m, StockQuantity = 10 };
            SetupValidCart(product, 2);
            _shippingRateService.Setup(s => s.GetShippingFee("Cairo")).Returns(50m);

            var request = new PlaceOrderCommand { City = "Cairo", Address = "12 Tahrir Square", PaymentMethod = "Cash", IdempotencyKey = "key-1" };

            var result = await _placeOrderHandler.Handle(request, It.IsAny<CancellationToken>());

            Assert.That(result.IsSuccess, Is.True);
            _orders.Verify(o => o.AddAsync(It.Is<Order>(ord =>
                ord.TotalPrice == 350m &&
                ord.ShippingFee == 50m &&
                ord.OrderStatus == Domain.Enums.OrderStatus.Processing &&
                ord.ShippingAddress == "12 Tahrir Square"), It.IsAny<CancellationToken>()), Times.Once);
            _orderItems.Verify(oi => oi.AddRangeAsync(It.Is<List<OrderItem>>(items => items.Count == 1 && items[0].UnitPrice == 150m && items[0].Quantity == 2), It.IsAny<CancellationToken>()), Times.Once);
            _payments.Verify(p => p.AddAsync(It.Is<Payment>(pay =>
                pay.Amount == 350d &&
                pay.PaymentMethod == "Cash" &&
                pay.PaymentStatus == "Pending"), It.IsAny<CancellationToken>()), Times.Once);
            _idempotencyRecords.Verify(r => r.AddAsync(It.Is<IdempotencyRecord>(rec => rec.IdempotencyKey == "key-1" && rec.UserId == _userId), It.IsAny<CancellationToken>()), Times.Once);
            _products.Verify(p => p.Update(It.Is<Product>(prod => prod.StockQuantity == 8)), Times.Once);
            _cartItems.Verify(c => c.Delete(It.IsAny<CartItem>()), Times.Once);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task EnsurePlaceOrderHandlerLeavesPaymentPendingUntilPaymobConfirmsForVisa()
        {
            var product = new Product { ProductId = Guid.NewGuid(), ProductName = "Sofa", Price = 150m, StockQuantity = 10 };
            SetupValidCart(product, 2);
            _shippingRateService.Setup(s => s.GetShippingFee(It.IsAny<string>())).Returns(50m);

            var request = new PlaceOrderCommand { City = "Cairo", PaymentMethod = "Visa" };

            await _placeOrderHandler.Handle(request, It.IsAny<CancellationToken>());

            _payments.Verify(p => p.AddAsync(It.Is<Payment>(pay => pay.PaymentStatus == "Pending"), It.IsAny<CancellationToken>()), Times.Once);
            _idempotencyRecords.Verify(r => r.AddAsync(It.IsAny<IdempotencyRecord>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task EnsurePlaceOrderHandlerAppliesCouponDiscountWhenCouponCodeIsValid()
        {
            var product = new Product { ProductId = Guid.NewGuid(), ProductName = "Desk", Price = 100m, StockQuantity = 10 };
            SetupValidCart(product, 1);
            _shippingRateService.Setup(s => s.GetShippingFee(It.IsAny<string>())).Returns(30m);
            _coupons.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Coupon, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Coupon { CouponCode = "SAVE10", CouponRatio = 10m, ExpireDate = DateTime.UtcNow.AddDays(1) });

            var request = new PlaceOrderCommand { City = "Cairo", PaymentMethod = "Cash", CouponCode = "SAVE10" };

            await _placeOrderHandler.Handle(request, It.IsAny<CancellationToken>());

            _orders.Verify(o => o.AddAsync(It.Is<Order>(ord => ord.TotalPrice == 120m && ord.CouponDiscount == 10m), It.IsAny<CancellationToken>()), Times.Once);
            _orderItems.Verify(oi => oi.AddRangeAsync(It.Is<List<OrderItem>>(items => items[0].UnitPrice == 90m), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task EnsurePlaceOrderHandlerRejectsClientSuppliedDiscountWithoutCouponCode()
        {
            var product = new Product { ProductId = Guid.NewGuid(), ProductName = "Desk", Price = 100m, StockQuantity = 10 };
            SetupValidCart(product, 1);
            _shippingRateService.Setup(s => s.GetShippingFee(It.IsAny<string>())).Returns(30m);

            var result = await _placeOrderHandler.Handle(
                new PlaceOrderCommand { City = "Cairo", PaymentMethod = "Cash", OrderDiscount = 10m },
                It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Errors, Does.Contain("A valid coupon code is required to apply a discount."));
            });
            _orders.Verify(o => o.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task EnsurePlaceOrderHandlerReturnsFailureWhenCouponIsExpired()
        {
            var product = new Product { ProductId = Guid.NewGuid(), ProductName = "Desk", Price = 100m, StockQuantity = 10 };
            SetupValidCart(product, 1);
            _shippingRateService.Setup(s => s.GetShippingFee(It.IsAny<string>())).Returns(30m);
            _coupons.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Coupon, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Coupon { CouponCode = "OLD10", CouponRatio = 10m, ExpireDate = DateTime.UtcNow.AddDays(-1) });

            var result = await _placeOrderHandler.Handle(
                new PlaceOrderCommand { City = "Cairo", PaymentMethod = "Cash", CouponCode = "OLD10" },
                It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Errors, Does.Contain("Coupon has expired."));
            });
            _orders.Verify(o => o.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task EnsurePlaceOrderHandlerAppliesFullDiscountWhenCouponRatioIsOneHundred()
        {
            var product = new Product { ProductId = Guid.NewGuid(), ProductName = "Desk", Price = 100m, StockQuantity = 10 };
            SetupValidCart(product, 1);
            _shippingRateService.Setup(s => s.GetShippingFee(It.IsAny<string>())).Returns(30m);
            _coupons.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Coupon, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Coupon { CouponCode = "FREE", CouponRatio = 100m, ExpireDate = DateTime.UtcNow.AddDays(1) });

            await _placeOrderHandler.Handle(
                new PlaceOrderCommand { City = "Cairo", PaymentMethod = "Cash", CouponCode = "FREE" },
                It.IsAny<CancellationToken>());

            _orders.Verify(o => o.AddAsync(It.Is<Order>(ord => ord.TotalPrice == 30m), It.IsAny<CancellationToken>()), Times.Once);
            _orderItems.Verify(oi => oi.AddRangeAsync(It.Is<List<OrderItem>>(items => items[0].UnitPrice == 0m), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task EnsurePlaceOrderHandlerUsesUserCityWhenRequestCityIsMissing()
        {
            var product = new Product { ProductId = Guid.NewGuid(), ProductName = "Sofa", Price = 150m, StockQuantity = 10 };
            SetupValidCart(product, 1);
            _shippingRateService.Setup(s => s.GetShippingFee("Cairo")).Returns(50m);

            var request = new PlaceOrderCommand { PaymentMethod = "Cash" };

            await _placeOrderHandler.Handle(request, It.IsAny<CancellationToken>());

            _shippingRateService.Verify(s => s.GetShippingFee("Cairo"), Times.Once);
            _orders.Verify(o => o.AddAsync(It.Is<Order>(ord => ord.ShippingAddress == "12 Tahrir Square"), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task EnsurePlaceOrderHandlerDeactivatesProductWhenStockReachesZero()
        {
            var product = new Product { ProductId = Guid.NewGuid(), ProductName = "Sofa", Price = 150m, StockQuantity = 2 };
            SetupValidCart(product, 2);
            _shippingRateService.Setup(s => s.GetShippingFee(It.IsAny<string>())).Returns(50m);

            var request = new PlaceOrderCommand { City = "Cairo", PaymentMethod = "Cash" };

            await _placeOrderHandler.Handle(request, It.IsAny<CancellationToken>());

            _products.Verify(p => p.Update(It.Is<Product>(prod => prod.StockQuantity == 0 && prod.IsActive == false)), Times.Once);
        }

        [Test]
        public async Task EnsurePlaceOrderHandlerReturnsFailureWhenCartIsEmpty()
        {
            _identityService.Setup(i => i.GetUserByIdAsync(_userId)).ReturnsAsync(ValidUser());
            _shoppingCarts.Setup(s => s.FindAsync(It.IsAny<Expression<Func<ShoppingCart, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ShoppingCart { Id = Guid.NewGuid(), UserId = _userId });
            _cartItems.Setup(c => c.FindAllAsync(It.IsAny<Expression<Func<CartItem, bool>>>(), It.IsAny<Expression<Func<CartItem, object>>[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<CartItem>());

            var result = await _placeOrderHandler.Handle(new PlaceOrderCommand { PaymentMethod = "Cash" }, It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Errors, Has.Some.EqualTo("The shopping cart is empty."));
            });
            _orders.Verify(o => o.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task EnsurePlaceOrderHandlerReturnsFailureWhenProductIsOutOfStock()
        {
            var product = new Product { ProductId = Guid.NewGuid(), ProductName = "Sofa", Price = 150m, StockQuantity = 1 };
            SetupValidCart(product, 2);

            var result = await _placeOrderHandler.Handle(new PlaceOrderCommand { PaymentMethod = "Cash" }, It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Errors, Has.Some.EqualTo("Product Sofa is out of stock."));
            });
            _orders.Verify(o => o.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task EnsurePlaceOrderHandlerReturnsExistingOrderIdWhenIdempotencyKeyIsAlreadyProcessed()
        {
            var existingOrderId = Guid.NewGuid();
            _idempotencyRecords.Setup(r => r.FindAsync(It.IsAny<Expression<Func<IdempotencyRecord, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new IdempotencyRecord { UserId = _userId, IdempotencyKey = "key-1", OrderId = existingOrderId });

            var result = await _placeOrderHandler.Handle(new PlaceOrderCommand { PaymentMethod = "Cash", IdempotencyKey = "key-1" }, It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.True);
                Assert.That(result.Value, Is.EqualTo(existingOrderId));
            });
            _orders.Verify(o => o.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task EnsurePlaceOrderHandlerReturnsFailureWhenConcurrencyConflictOccurs()
        {
            var product = new Product { ProductId = Guid.NewGuid(), ProductName = "Sofa", Price = 150m, StockQuantity = 10 };
            SetupValidCart(product, 1);
            _shippingRateService.Setup(s => s.GetShippingFee(It.IsAny<string>())).Returns(50m);
            _unitOfWork.Setup(u => u.CompleteAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateConcurrencyException());

            var result = await _placeOrderHandler.Handle(new PlaceOrderCommand { PaymentMethod = "Cash" }, It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Errors, Has.Some.EqualTo("Product stock changed while placing your order. Please review your cart and try again."));
            });
        }

        [Test]
        public async Task EnsurePlaceOrderHandlerReturnsExistingOrderIdWhenDuplicateRequestWinsConcurrently()
        {
            var product = new Product { ProductId = Guid.NewGuid(), ProductName = "Sofa", Price = 150m, StockQuantity = 10 };
            SetupValidCart(product, 1);
            _shippingRateService.Setup(s => s.GetShippingFee(It.IsAny<string>())).Returns(50m);

            var existingOrderId = Guid.NewGuid();
            _idempotencyRecords.Setup(r => r.FindAsync(It.IsAny<Expression<Func<IdempotencyRecord, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new IdempotencyRecord { UserId = _userId, IdempotencyKey = "key-1", OrderId = existingOrderId });
            _unitOfWork.Setup(u => u.CompleteAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateException("duplicate key"));

            var result = await _placeOrderHandler.Handle(new PlaceOrderCommand { PaymentMethod = "Cash", IdempotencyKey = "key-1" }, It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.True);
                Assert.That(result.Value, Is.EqualTo(existingOrderId));
            });
        }

        [Test]
        public void EnsurePlaceOrderHandlerThrowsNotFoundWhenUserIsNull()
        {
            _currentUserService.SetupGet(c => c.UserId).Returns((Guid?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _placeOrderHandler.Handle(new PlaceOrderCommand { PaymentMethod = "Cash" }, It.IsAny<CancellationToken>()));
        }

        [Test]
        public void EnsurePlaceOrderHandlerThrowsNotFoundWhenUserNotFound()
        {
            _identityService.Setup(i => i.GetUserByIdAsync(_userId)).ReturnsAsync((AppUser?)null);
            _shoppingCarts.Setup(s => s.FindAsync(It.IsAny<Expression<Func<ShoppingCart, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ShoppingCart { Id = Guid.NewGuid(), UserId = _userId });

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _placeOrderHandler.Handle(new PlaceOrderCommand { PaymentMethod = "Cash" }, It.IsAny<CancellationToken>()));
        }

        [Test]
        public void EnsurePlaceOrderHandlerThrowsNotFoundWhenShoppingCartNotFound()
        {
            _identityService.Setup(i => i.GetUserByIdAsync(_userId)).ReturnsAsync(ValidUser());
            _shoppingCarts.Setup(s => s.FindAsync(It.IsAny<Expression<Func<ShoppingCart, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ShoppingCart?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _placeOrderHandler.Handle(new PlaceOrderCommand { PaymentMethod = "Cash" }, It.IsAny<CancellationToken>()));
        }
    }
}
