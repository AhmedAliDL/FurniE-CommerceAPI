using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Repos;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Wishlists.Command.AddToWishlist;
using App.Application.Wishlists.Command.MoveWishlistItemToCart;
using App.Application.Wishlists.Command.RemoveFromWishlist;
using App.Application.Wishlists.Queries.GetWishlist;
using App.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Linq.Expressions;

namespace App.Test.Handlers
{
    [TestFixture]
    [Category("Wishlists")]
    public class WishlistsHandlerTest
    {
        private Mock<IUnitOfWork> _unitOfWork = null!;
        private Mock<IBaseRepository<Product>> _products = null!;
        private Mock<IBaseRepository<WishlistItem>> _wishlistItems = null!;
        private Mock<IBaseRepository<CartItem>> _cartItems = null!;
        private Mock<IBaseRepository<ShoppingCart>> _shoppingCarts = null!;
        private Mock<ICurrentUserService> _currentUserService = null!;
        private GetWishlistHandler _getWishlistHandler = null!;
        private AddToWishlistHandler _addToWishlistHandler = null!;
        private RemoveFromWishlistHandler _removeFromWishlistHandler = null!;
        private MoveWishlistItemToCartHandler _moveToCartHandler = null!;

        [SetUp]
        public void Setup()
        {
            _unitOfWork = new Mock<IUnitOfWork>();
            _products = new Mock<IBaseRepository<Product>>();
            _wishlistItems = new Mock<IBaseRepository<WishlistItem>>();
            _cartItems = new Mock<IBaseRepository<CartItem>>();
            _shoppingCarts = new Mock<IBaseRepository<ShoppingCart>>();
            _currentUserService = new Mock<ICurrentUserService>();
            _unitOfWork.Setup(u => u.Products).Returns(_products.Object);
            _unitOfWork.Setup(u => u.WishlistItems).Returns(_wishlistItems.Object);
            _unitOfWork.Setup(u => u.CartItems).Returns(_cartItems.Object);
            _unitOfWork.Setup(u => u.ShoppingCarts).Returns(_shoppingCarts.Object);

            _getWishlistHandler = new GetWishlistHandler(_unitOfWork.Object, _currentUserService.Object, new Mock<ILogger<GetWishlistHandler>>().Object);
            _addToWishlistHandler = new AddToWishlistHandler(_unitOfWork.Object, _currentUserService.Object, new Mock<ILogger<AddToWishlistHandler>>().Object);
            _removeFromWishlistHandler = new RemoveFromWishlistHandler(_unitOfWork.Object, _currentUserService.Object, new Mock<ILogger<RemoveFromWishlistHandler>>().Object);
            _moveToCartHandler = new MoveWishlistItemToCartHandler(_unitOfWork.Object, _currentUserService.Object, new Mock<ILogger<MoveWishlistItemToCartHandler>>().Object);
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
        public async Task EnsureGetWishlistHandlerReturnsDtosWhenWishlistHasItems()
        {
            var userId = Guid.NewGuid();
            var product = ValidProduct();
            _currentUserService.Setup(s => s.UserId).Returns(userId);
            _wishlistItems.Setup(r => r.FindAllAsync(
                    It.IsAny<Expression<Func<WishlistItem, bool>>>(),
                    It.IsAny<Expression<Func<WishlistItem, object>>[]>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync([new WishlistItem { UserId = userId, ProductId = product.ProductId, ProductOriginalPrice = 150m, Product = product }]);

            var result = await _getWishlistHandler.Handle(It.IsAny<GetWishlistQuery>(), It.IsAny<CancellationToken>());

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].ProductId, Is.EqualTo(product.ProductId));
            Assert.That(result[0].ProductName, Is.EqualTo("Wooden Chair"));
            Assert.That(result[0].Price, Is.EqualTo(150m));
            Assert.That(result[0].IsAvailable, Is.True);
        }

        [Test]
        public void EnsureGetWishlistHandlerThrowsNotFoundWhenUserNotResolved()
        {
            _currentUserService.Setup(s => s.UserId).Returns((Guid?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _getWishlistHandler.Handle(It.IsAny<GetWishlistQuery>(), It.IsAny<CancellationToken>()));
        }

        [Test]
        public async Task EnsureAddToWishlistHandlerAddsItemWhenProductIsNew()
        {
            var userId = Guid.NewGuid();
            var product = ValidProduct();
            _currentUserService.Setup(s => s.UserId).Returns(userId);
            _products.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Product, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(product);
            _wishlistItems.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<WishlistItem, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((WishlistItem?)null);
            _wishlistItems.Setup(r => r.AddAsync(
                    It.IsAny<WishlistItem>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _unitOfWork.Setup(u => u.CompleteAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            await _addToWishlistHandler.Handle(new AddToWishlistCommand(product.ProductId, 150m), It.IsAny<CancellationToken>());

            _wishlistItems.Verify(r => r.AddAsync(It.Is<WishlistItem>(w => w.UserId == userId && w.ProductId == product.ProductId && w.ProductOriginalPrice == 150m), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task EnsureAddToWishlistHandlerSkipsAddWhenProductAlreadyInWishlist()
        {
            var userId = Guid.NewGuid();
            var product = ValidProduct();
            _currentUserService.Setup(s => s.UserId).Returns(userId);
            _products.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Product, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(product);
            _wishlistItems.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<WishlistItem, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WishlistItem { UserId = userId, ProductId = product.ProductId });

            await _addToWishlistHandler.Handle(new AddToWishlistCommand(product.ProductId, 150m), It.IsAny<CancellationToken>());

            _wishlistItems.Verify(r => r.AddAsync(It.IsAny<WishlistItem>(), It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public void EnsureAddToWishlistHandlerThrowsNotFoundWhenProductNotExists()
        {
            _currentUserService.Setup(s => s.UserId).Returns(Guid.NewGuid());
            _products.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Product, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Product?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _addToWishlistHandler.Handle(new AddToWishlistCommand(Guid.NewGuid(), 150m), It.IsAny<CancellationToken>()));
        }

        [Test]
        public async Task EnsureRemoveFromWishlistHandlerDeletesItemWhenItExists()
        {
            var userId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            _currentUserService.Setup(s => s.UserId).Returns(userId);
            _wishlistItems.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<WishlistItem, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WishlistItem { UserId = userId, ProductId = productId });
            _unitOfWork.Setup(u => u.CompleteAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            await _removeFromWishlistHandler.Handle(new RemoveFromWishlistCommand(productId), It.IsAny<CancellationToken>());

            _wishlistItems.Verify(r => r.Delete(It.IsAny<WishlistItem>()), Times.Once);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void EnsureRemoveFromWishlistHandlerThrowsNotFoundWhenItemNotExists()
        {
            _currentUserService.Setup(s => s.UserId).Returns(Guid.NewGuid());
            _wishlistItems.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<WishlistItem, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((WishlistItem?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _removeFromWishlistHandler.Handle(new RemoveFromWishlistCommand(Guid.NewGuid()), It.IsAny<CancellationToken>()));
        }

        [Test]
        public async Task EnsureMoveToCartHandlerAddsItemAndRemovesItFromWishlist()
        {
            var userId = Guid.NewGuid();
            var cartId = Guid.NewGuid();
            var product = ValidProduct();
            _currentUserService.Setup(s => s.UserId).Returns(userId);
            _wishlistItems.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<WishlistItem, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WishlistItem { UserId = userId, ProductId = product.ProductId });
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
            _unitOfWork.Setup(u => u.CompleteAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var result = await _moveToCartHandler.Handle(new MoveWishlistItemToCartCommand(product.ProductId), It.IsAny<CancellationToken>());

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.EqualTo("Item moved to cart"));
            _cartItems.Verify(r => r.AddAsync(It.Is<CartItem>(ci => ci.ShoppingCartId == cartId && ci.ProductId == product.ProductId && ci.ItemQuantity == 1), It.IsAny<CancellationToken>()), Times.Once);
            _wishlistItems.Verify(r => r.Delete(It.IsAny<WishlistItem>()), Times.Once);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task EnsureMoveToCartHandlerIncrementsQuantityWhenItemAlreadyInCart()
        {
            var userId = Guid.NewGuid();
            var cartId = Guid.NewGuid();
            var product = ValidProduct();
            _currentUserService.Setup(s => s.UserId).Returns(userId);
            _wishlistItems.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<WishlistItem, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WishlistItem { UserId = userId, ProductId = product.ProductId });
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

            var result = await _moveToCartHandler.Handle(new MoveWishlistItemToCartCommand(product.ProductId), It.IsAny<CancellationToken>());

            Assert.That(result.IsSuccess, Is.True);
            _cartItems.Verify(r => r.Update(It.Is<CartItem>(ci => ci.ItemQuantity == 2)), Times.Once);
            _cartItems.Verify(r => r.AddAsync(It.IsAny<CartItem>(), It.IsAny<CancellationToken>()), Times.Never);
            _wishlistItems.Verify(r => r.Delete(It.IsAny<WishlistItem>()), Times.Once);
        }

        [Test]
        public async Task EnsureMoveToCartHandlerCreatesCartWhenUserHasNoCart()
        {
            var userId = Guid.NewGuid();
            var product = ValidProduct();
            _currentUserService.Setup(s => s.UserId).Returns(userId);
            _wishlistItems.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<WishlistItem, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WishlistItem { UserId = userId, ProductId = product.ProductId });
            _products.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Product, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(product);
            _shoppingCarts.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<ShoppingCart, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((ShoppingCart?)null);
            _cartItems.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<CartItem, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((CartItem?)null);
            _unitOfWork.Setup(u => u.CompleteAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var result = await _moveToCartHandler.Handle(new MoveWishlistItemToCartCommand(product.ProductId), It.IsAny<CancellationToken>());

            Assert.That(result.IsSuccess, Is.True);
            _shoppingCarts.Verify(r => r.AddAsync(It.Is<ShoppingCart>(c => c.UserId == userId), It.IsAny<CancellationToken>()), Times.Once);
            _cartItems.Verify(r => r.AddAsync(It.IsAny<CartItem>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task EnsureMoveToCartHandlerReturnsFailureWhenProductOutOfStock()
        {
            var userId = Guid.NewGuid();
            var product = ValidProduct();
            product.StockQuantity = 0;
            _currentUserService.Setup(s => s.UserId).Returns(userId);
            _wishlistItems.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<WishlistItem, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WishlistItem { UserId = userId, ProductId = product.ProductId });
            _products.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Product, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(product);

            var result = await _moveToCartHandler.Handle(new MoveWishlistItemToCartCommand(product.ProductId), It.IsAny<CancellationToken>());

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Errors, Does.Contain("Product out of stock"));
            _wishlistItems.Verify(r => r.Delete(It.IsAny<WishlistItem>()), Times.Never);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public void EnsureMoveToCartHandlerThrowsNotFoundWhenWishlistItemNotExists()
        {
            _currentUserService.Setup(s => s.UserId).Returns(Guid.NewGuid());
            _wishlistItems.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<WishlistItem, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((WishlistItem?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _moveToCartHandler.Handle(new MoveWishlistItemToCartCommand(Guid.NewGuid()), It.IsAny<CancellationToken>()));
        }

        [Test]
        public void EnsureMoveToCartHandlerThrowsNotFoundWhenProductNotExists()
        {
            var userId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            _currentUserService.Setup(s => s.UserId).Returns(userId);
            _wishlistItems.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<WishlistItem, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WishlistItem { UserId = userId, ProductId = productId });
            _products.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Product, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Product?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _moveToCartHandler.Handle(new MoveWishlistItemToCartCommand(productId), It.IsAny<CancellationToken>()));
        }
    }
}
