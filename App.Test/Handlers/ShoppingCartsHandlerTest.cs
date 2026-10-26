using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Repos;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.ShoppingCarts.Command.AddCartItem;
using App.Application.ShoppingCarts.Command.ClearCart;
using App.Application.ShoppingCarts.Command.DeleteCartItem;
using App.Application.ShoppingCarts.Command.UpdateCartQuantities;
using App.Application.ShoppingCarts.Queries.GetCartItems;
using App.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Linq.Expressions;

namespace App.Test.Handlers
{
    [TestFixture]
    [Category("ShoppingCarts")]
    public class ShoppingCartsHandlerTest
    {
        private Mock<IUnitOfWork> _unitOfWork = null!;
        private Mock<IBaseRepository<Product>> _products = null!;
        private Mock<IBaseRepository<CartItem>> _cartItems = null!;
        private Mock<IBaseRepository<ShoppingCart>> _shoppingCarts = null!;
        private Mock<ICurrentUserService> _currentUserService = null!;
        private GetCartItemsHandler _getCartItemsHandler = null!;
        private AddCartItemHandler _addCartItemHandler = null!;
        private DeleteCartItemHandler _deleteCartItemHandler = null!;
        private UpdateCartQuantitiesHandler _updateCartQuantitiesHandler = null!;
        private ClearCartHandler _clearCartHandler = null!;

        [SetUp]
        public void Setup()
        {
            _unitOfWork = new Mock<IUnitOfWork>();
            _products = new Mock<IBaseRepository<Product>>();
            _cartItems = new Mock<IBaseRepository<CartItem>>();
            _shoppingCarts = new Mock<IBaseRepository<ShoppingCart>>();
            _currentUserService = new Mock<ICurrentUserService>();
            _unitOfWork.Setup(u => u.Products).Returns(_products.Object);
            _unitOfWork.Setup(u => u.CartItems).Returns(_cartItems.Object);
            _unitOfWork.Setup(u => u.ShoppingCarts).Returns(_shoppingCarts.Object);

            _getCartItemsHandler = new GetCartItemsHandler(_unitOfWork.Object, _currentUserService.Object, new Mock<ILogger<GetCartItemsHandler>>().Object);
            _addCartItemHandler = new AddCartItemHandler(_unitOfWork.Object, _currentUserService.Object, new Mock<ILogger<AddCartItemHandler>>().Object);
            _deleteCartItemHandler = new DeleteCartItemHandler(_unitOfWork.Object, new Mock<ILogger<DeleteCartItemHandler>>().Object);
            _updateCartQuantitiesHandler = new UpdateCartQuantitiesHandler(_unitOfWork.Object, new Mock<ILogger<UpdateCartQuantitiesHandler>>().Object);
            _clearCartHandler = new ClearCartHandler(_unitOfWork.Object, _currentUserService.Object, new Mock<ILogger<ClearCartHandler>>().Object);
        }

        private static Product ValidProduct() => new()
        {
            ProductId = Guid.NewGuid(),
            ProductName = "Wooden Chair",
            Description = "A comfortable wooden chair",
            Price = 150m,
            StockQuantity = 10,
            ImageUrl = "https://example.com/chair.jpg",
            IsActive = true
        };

        [Test]
        public async Task EnsureGetCartItemsHandlerReturnsCartWithTotalPriceWhenCartHasItems()
        {
            var userId = Guid.NewGuid();
            var cartId = Guid.NewGuid();
            var product = ValidProduct();
            _currentUserService.Setup(s => s.UserId).Returns(userId);
            _shoppingCarts.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<ShoppingCart, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ShoppingCart { Id = cartId, UserId = userId });
            _cartItems.Setup(r => r.FindAllAsync(
                    It.IsAny<Expression<Func<CartItem, bool>>>(),
                    It.IsAny<Expression<Func<CartItem, object>>[]>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync([new CartItem { ShoppingCartId = cartId, ProductId = product.ProductId, ItemQuantity = 2, Product = product }]);

            var result = await _getCartItemsHandler.Handle(It.IsAny<GetCartItemsQuery>(), It.IsAny<CancellationToken>());

            Assert.That(result.ShoppingCartId, Is.EqualTo(cartId));
            Assert.That(result.CartItems.Count, Is.EqualTo(1));
            Assert.That(result.CartItems[0].ProductName, Is.EqualTo("Wooden Chair"));
            Assert.That(result.CartItems[0].TotalPrice, Is.EqualTo(300m));
            Assert.That(result.TotalPrice, Is.EqualTo(300m));
        }

        [Test]
        public void EnsureGetCartItemsHandlerThrowsNotFoundWhenUserNotResolved()
        {
            _currentUserService.Setup(s => s.UserId).Returns((Guid?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _getCartItemsHandler.Handle(It.IsAny<GetCartItemsQuery>(), It.IsAny<CancellationToken>()));
        }

        [Test]
        public async Task EnsureAddCartItemHandlerAddsNewItemWhenCartExists()
        {
            var userId = Guid.NewGuid();
            var cartId = Guid.NewGuid();
            var product = ValidProduct();
            _currentUserService.Setup(s => s.UserId).Returns(userId);
            _products.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Product, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(product);
            _shoppingCarts.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<ShoppingCart, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ShoppingCart { Id = cartId, UserId = userId });
            _cartItems.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<CartItem, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((CartItem?)null);
            _cartItems.Setup(r => r.AddAsync(
                    It.IsAny<CartItem>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _unitOfWork.Setup(u => u.CompleteAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var result = await _addCartItemHandler.Handle(new AddCartItemCommand(product.ProductId), It.IsAny<CancellationToken>());

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.EqualTo("Item added to cart"));
            _cartItems.Verify(r => r.AddAsync(It.Is<CartItem>(ci => ci.ShoppingCartId == cartId && ci.ProductId == product.ProductId && ci.ItemQuantity == 1), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task EnsureAddCartItemHandlerCreatesCartWhenUserHasNoCart()
        {
            var userId = Guid.NewGuid();
            var product = ValidProduct();
            _currentUserService.Setup(s => s.UserId).Returns(userId);
            _products.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Product, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(product);
            _shoppingCarts.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<ShoppingCart, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((ShoppingCart?)null);
            _shoppingCarts.Setup(r => r.AddAsync(
                    It.IsAny<ShoppingCart>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _cartItems.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<CartItem, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((CartItem?)null);
            _cartItems.Setup(r => r.AddAsync(
                    It.IsAny<CartItem>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _unitOfWork.Setup(u => u.CompleteAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var result = await _addCartItemHandler.Handle(new AddCartItemCommand(product.ProductId), It.IsAny<CancellationToken>());

            Assert.That(result.IsSuccess, Is.True);
            _shoppingCarts.Verify(r => r.AddAsync(It.Is<ShoppingCart>(c => c.UserId == userId), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task EnsureAddCartItemHandlerIncrementsQuantityWhenItemAlreadyInCart()
        {
            var userId = Guid.NewGuid();
            var cartId = Guid.NewGuid();
            var product = ValidProduct();
            _currentUserService.Setup(s => s.UserId).Returns(userId);
            _products.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Product, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(product);
            _shoppingCarts.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<ShoppingCart, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ShoppingCart { Id = cartId, UserId = userId });
            _cartItems.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<CartItem, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CartItem { ShoppingCartId = cartId, ProductId = product.ProductId, ItemQuantity = 1 });
            _unitOfWork.Setup(u => u.CompleteAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var result = await _addCartItemHandler.Handle(new AddCartItemCommand(product.ProductId), It.IsAny<CancellationToken>());

            Assert.That(result.IsSuccess, Is.True);
            _cartItems.Verify(r => r.Update(It.Is<CartItem>(ci => ci.ItemQuantity == 2)), Times.Once);
            _cartItems.Verify(r => r.AddAsync(It.IsAny<CartItem>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task EnsureAddCartItemHandlerReturnsFailureWhenProductOutOfStock()
        {
            var product = ValidProduct();
            product.StockQuantity = 0;
            _products.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Product, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(product);

            var result = await _addCartItemHandler.Handle(new AddCartItemCommand(product.ProductId), It.IsAny<CancellationToken>());

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Errors, Does.Contain("Product out of stock"));
        }

        [Test]
        public void EnsureAddCartItemHandlerThrowsNotFoundWhenProductNotExists()
        {
            _products.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Product, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Product?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _addCartItemHandler.Handle(new AddCartItemCommand(Guid.NewGuid()), It.IsAny<CancellationToken>()));
        }

        [Test]
        public async Task EnsureDeleteCartItemHandlerDeletesItemWhenItExists()
        {
            _cartItems.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<CartItem, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CartItem { ShoppingCartId = Guid.NewGuid(), ProductId = Guid.NewGuid(), ItemQuantity = 1 });
            _unitOfWork.Setup(u => u.CompleteAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            await _deleteCartItemHandler.Handle(new DeleteCartItemCommand(Guid.NewGuid(), Guid.NewGuid()), It.IsAny<CancellationToken>());

            _cartItems.Verify(r => r.Delete(It.IsAny<CartItem>()), Times.Once);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task EnsureDeleteCartItemHandlerSkipsDeleteWhenItemNotExists()
        {
            _cartItems.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<CartItem, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((CartItem?)null);

            await _deleteCartItemHandler.Handle(new DeleteCartItemCommand(Guid.NewGuid(), Guid.NewGuid()), It.IsAny<CancellationToken>());

            _cartItems.Verify(r => r.Delete(It.IsAny<CartItem>()), Times.Never);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task EnsureUpdateCartQuantitiesHandlerUpdatesQuantitiesWhenItemsExist()
        {
            var cartId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            _cartItems.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<CartItem, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CartItem { ShoppingCartId = cartId, ProductId = productId, ItemQuantity = 1 });
            _unitOfWork.Setup(u => u.CompleteAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            await _updateCartQuantitiesHandler.Handle(
                new UpdateCartQuantitiesCommand
                {
                    ShoppingCartId = cartId,
                    Items = [new CartItemQuantity { ProductId = productId, Quantity = 5 }]
                },
                It.IsAny<CancellationToken>());

            _cartItems.Verify(r => r.Update(It.Is<CartItem>(ci => ci.ItemQuantity == 5)), Times.Once);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task EnsureUpdateCartQuantitiesHandlerSkipsMissingItems()
        {
            _cartItems.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<CartItem, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((CartItem?)null);
            _unitOfWork.Setup(u => u.CompleteAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            await _updateCartQuantitiesHandler.Handle(
                new UpdateCartQuantitiesCommand
                {
                    ShoppingCartId = Guid.NewGuid(),
                    Items = [new CartItemQuantity { ProductId = Guid.NewGuid(), Quantity = 5 }]
                },
                It.IsAny<CancellationToken>());

            _cartItems.Verify(r => r.Update(It.IsAny<CartItem>()), Times.Never);
        }

        [Test]
        public async Task EnsureClearCartHandlerDeletesAllItemsWhenCartHasItems()
        {
            var userId = Guid.NewGuid();
            var cartId = Guid.NewGuid();
            _currentUserService.Setup(s => s.UserId).Returns(userId);
            _shoppingCarts.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<ShoppingCart, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ShoppingCart { Id = cartId, UserId = userId });
            _cartItems.Setup(r => r.FindAllAsync(
                    It.IsAny<Expression<Func<CartItem, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                [
                    new CartItem { ShoppingCartId = cartId, ProductId = Guid.NewGuid(), ItemQuantity = 1 },
                    new CartItem { ShoppingCartId = cartId, ProductId = Guid.NewGuid(), ItemQuantity = 3 }
                ]);
            _unitOfWork.Setup(u => u.CompleteAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(2);

            await _clearCartHandler.Handle(new ClearCartCommand(), It.IsAny<CancellationToken>());

            _cartItems.Verify(r => r.DeleteRange(It.Is<IEnumerable<CartItem>>(items => items.Count() == 2)), Times.Once);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task EnsureClearCartHandlerSkipsDeleteWhenCartIsEmpty()
        {
            var userId = Guid.NewGuid();
            var cartId = Guid.NewGuid();
            _currentUserService.Setup(s => s.UserId).Returns(userId);
            _shoppingCarts.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<ShoppingCart, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ShoppingCart { Id = cartId, UserId = userId });
            _cartItems.Setup(r => r.FindAllAsync(
                    It.IsAny<Expression<Func<CartItem, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            await _clearCartHandler.Handle(new ClearCartCommand(), It.IsAny<CancellationToken>());

            _cartItems.Verify(r => r.DeleteRange(It.IsAny<IEnumerable<CartItem>>()), Times.Never);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task EnsureClearCartHandlerSkipsDeleteWhenUserHasNoCart()
        {
            _currentUserService.Setup(s => s.UserId).Returns(Guid.NewGuid());
            _shoppingCarts.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<ShoppingCart, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((ShoppingCart?)null);

            await _clearCartHandler.Handle(new ClearCartCommand(), It.IsAny<CancellationToken>());

            _cartItems.Verify(r => r.DeleteRange(It.IsAny<IEnumerable<CartItem>>()), Times.Never);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public void EnsureClearCartHandlerThrowsNotFoundWhenUserNotResolved()
        {
            _currentUserService.Setup(s => s.UserId).Returns((Guid?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _clearCartHandler.Handle(new ClearCartCommand(), It.IsAny<CancellationToken>()));
        }
    }
}
