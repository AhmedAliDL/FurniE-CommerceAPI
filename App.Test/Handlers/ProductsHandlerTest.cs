using App.Application.Common.Enums;
using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Repos;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Products.Command.CreateProduct;
using App.Application.Products.Command.DeleteProduct;
using App.Application.Products.Command.UpdateProduct;
using App.Application.Products.Queries.GetAllProducts;
using App.Application.Products.Queries.GetActiveProducts;
using App.Application.Products.Queries.GetProductById;
using App.Application.Products.Queries.GetProductDetails;
using App.Application.Products.Queries.GetProductsByCategory;
using App.Application.Products.Queries.GetRecentlyViewed;
using App.Application.Products.Queries.SearchProducts;
using App.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Linq.Expressions;

namespace App.Test.Handlers
{
    [TestFixture]
    [Category("Products")]
    public class ProductsHandlerTest
    {
        private Mock<IUnitOfWork> _unitOfWork = null!;
        private Mock<IBaseRepository<Product>> _products = null!;
        private Mock<IBaseRepository<Review>> _reviews = null!;
        private Mock<IBaseRepository<WishlistItem>> _wishlistItems = null!;
        private Mock<IBaseRepository<OrderItem>> _orderItems = null!;
        private Mock<ICurrentUserService> _currentUserService = null!;
        private Mock<IRecentlyViewedService> _recentlyViewedService = null!;
        private Mock<IPriceDropDetectionBackgroundJob> _priceDropDetection = null!;

        private GetAllProductsHandler _getAllHandler = null!;
        private GetActiveProductsHandler _getActiveHandler = null!;
        private GetProductsByCategoryHandler _getByCategoryHandler = null!;
        private GetProductByIdHandler _getByIdHandler = null!;
        private GetProductDetailsHandler _getDetailsHandler = null!;
        private GetRecentlyViewedHandler _getRecentlyViewedHandler = null!;
        private SearchProductsHandler _searchHandler = null!;
        private CreateProductHandler _createHandler = null!;
        private UpdateProductHandler _updateHandler = null!;
        private DeleteProductHandler _deleteHandler = null!;

        [SetUp]
        public void Setup()
        {
            _unitOfWork = new Mock<IUnitOfWork>();
            _products = new Mock<IBaseRepository<Product>>();
            _reviews = new Mock<IBaseRepository<Review>>();
            _wishlistItems = new Mock<IBaseRepository<WishlistItem>>();
            _orderItems = new Mock<IBaseRepository<OrderItem>>();
            _currentUserService = new Mock<ICurrentUserService>();
            _recentlyViewedService = new Mock<IRecentlyViewedService>();
            _priceDropDetection = new Mock<IPriceDropDetectionBackgroundJob>();

            _unitOfWork.Setup(u => u.Products).Returns(_products.Object);
            _unitOfWork.Setup(u => u.Reviews).Returns(_reviews.Object);
            _unitOfWork.Setup(u => u.WishlistItems).Returns(_wishlistItems.Object);
            _unitOfWork.Setup(u => u.OrderItems).Returns(_orderItems.Object);

            _getAllHandler = new GetAllProductsHandler(_unitOfWork.Object, new Mock<ILogger<GetAllProductsHandler>>().Object);
            _getActiveHandler = new GetActiveProductsHandler(_unitOfWork.Object, new Mock<ILogger<GetActiveProductsHandler>>().Object);
            _getByCategoryHandler = new GetProductsByCategoryHandler(_unitOfWork.Object, new Mock<ILogger<GetProductsByCategoryHandler>>().Object);
            _getByIdHandler = new GetProductByIdHandler(_unitOfWork.Object, new Mock<ILogger<GetProductByIdHandler>>().Object);
            _getDetailsHandler = new GetProductDetailsHandler(_unitOfWork.Object, _currentUserService.Object, _recentlyViewedService.Object, new Mock<ILogger<GetProductDetailsHandler>>().Object);
            _getRecentlyViewedHandler = new GetRecentlyViewedHandler(_unitOfWork.Object, _currentUserService.Object, _recentlyViewedService.Object, new Mock<ILogger<GetRecentlyViewedHandler>>().Object);
            _searchHandler = new SearchProductsHandler(_unitOfWork.Object, new Mock<ILogger<SearchProductsHandler>>().Object);
            _createHandler = new CreateProductHandler(_unitOfWork.Object, new Mock<ILogger<CreateProductHandler>>().Object);
            _updateHandler = new UpdateProductHandler(_unitOfWork.Object, _priceDropDetection.Object, new Mock<ILogger<UpdateProductHandler>>().Object);
            _deleteHandler = new DeleteProductHandler(_unitOfWork.Object, new Mock<ILogger<DeleteProductHandler>>().Object);
        }

        private static Product ValidProduct()
        {
            var category = new Category { CategoryId = Guid.NewGuid(), CategoryName = "Chairs" };
            return new Product
            {
                ProductId = Guid.NewGuid(),
                ProductName = "Wooden Chair",
                Description = "A comfortable wooden chair",
                Price = 150m,
                StockQuantity = 10,
                CategoryId = category.CategoryId,
                ImageUrl = "https://example.com/chair.jpg",
                IsActive = true,
                Category = category,
                Reviews = []
            };
        }

        [Test]
        public async Task EnsureGetAllProductsHandlerReturnsResponsesWhenProductsExist()
        {
            _products.Setup(r => r.FindPagedAsync(
                    It.IsAny<Expression<Func<Product, bool>>>(),
                    It.IsAny<Expression<Func<Product, object>>[]>(),
                    It.IsAny<SortDirection>(),
                    It.IsAny<Expression<Func<Product, object>>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(((IReadOnlyList<Product>)[ValidProduct()], 1));

            var result = await _getAllHandler.Handle(new GetAllProductsQuery(), It.IsAny<CancellationToken>());

            Assert.That(result.TotalCount, Is.EqualTo(1));
            Assert.That(result.Items[0].ProductName, Is.EqualTo("Wooden Chair"));
            Assert.That(result.Items[0].CategoryName, Is.EqualTo("Chairs"));
            Assert.That(result.Items[0].IsAvailable, Is.True);
        }

        [Test]
        public async Task EnsureGetActiveProductsHandlerReturnsResponsesWhenActiveProductsExist()
        {
            _products.Setup(r => r.FindPagedAsync(
                    It.IsAny<Expression<Func<Product, bool>>>(),
                    It.IsAny<Expression<Func<Product, object>>[]>(),
                    It.IsAny<SortDirection>(),
                    It.IsAny<Expression<Func<Product, object>>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(((IReadOnlyList<Product>)[ValidProduct()], 1));

            var result = await _getActiveHandler.Handle(new GetActiveProductsQuery(), It.IsAny<CancellationToken>());

            Assert.That(result.TotalCount, Is.EqualTo(1));
            Assert.That(result.Items[0].IsActive, Is.True);
            Assert.That(result.Items[0].CategoryName, Is.EqualTo("Chairs"));
        }

        [Test]
        public async Task EnsureGetProductsByCategoryHandlerReturnsResponsesWhenCategoryHasProducts()
        {
            _products.Setup(r => r.FindPagedAsync(
                    It.IsAny<Expression<Func<Product, bool>>>(),
                    It.IsAny<Expression<Func<Product, object>>[]>(),
                    It.IsAny<SortDirection>(),
                    It.IsAny<Expression<Func<Product, object>>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(((IReadOnlyList<Product>)[ValidProduct()], 1));

            var result = await _getByCategoryHandler.Handle(new GetProductsByCategoryQuery("Chairs"), It.IsAny<CancellationToken>());

            Assert.That(result.TotalCount, Is.EqualTo(1));
            Assert.That(result.Items[0].CategoryName, Is.EqualTo("Chairs"));
        }

        [Test]
        public async Task EnsureGetProductByIdHandlerReturnsResponseWhenProductExists()
        {
            var product = ValidProduct();
            _products.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Product, bool>>>(),
                    It.IsAny<Expression<Func<Product, object>>[]>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(product);

            var result = await _getByIdHandler.Handle(new GetProductByIdQuery(product.ProductId), It.IsAny<CancellationToken>());

            Assert.That(result.ProductId, Is.EqualTo(product.ProductId));
            Assert.That(result.ProductName, Is.EqualTo("Wooden Chair"));
        }

        [Test]
        public void EnsureGetProductByIdHandlerThrowsNotFoundWhenProductNotExists()
        {
            _products.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Product, bool>>>(),
                    It.IsAny<Expression<Func<Product, object>>[]>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Product?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _getByIdHandler.Handle(new GetProductByIdQuery(Guid.NewGuid()), It.IsAny<CancellationToken>()));
        }

        [Test]
        public async Task EnsureGetProductDetailsHandlerRecordsViewWhenUserIsAuthenticated()
        {
            var product = ValidProduct();
            var userId = Guid.NewGuid();
            _products.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Product, bool>>>(),
                    It.IsAny<Expression<Func<Product, object>>[]>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(product);
            _currentUserService.Setup(s => s.UserId).Returns(userId);
            _recentlyViewedService.Setup(s => s.RecordViewAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _reviews.Setup(r => r.FindAllAsync(
                    It.IsAny<Expression<Func<Review, bool>>>(),
                    It.IsAny<Expression<Func<Review, object>>[]>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync([new Review
                {
                    ReviewId = Guid.NewGuid(),
                    ProductId = product.ProductId,
                    Rating = 5,
                    Comment = "Great chair",
                    Customer = new AppUser { FName = "Ahmed", LName = "Ali" }
                }]);

            var result = await _getDetailsHandler.Handle(new GetProductDetailsQuery(product.ProductId), It.IsAny<CancellationToken>());

            Assert.That(result.Product.ProductId, Is.EqualTo(product.ProductId));
            Assert.That(result.ReviewCount, Is.EqualTo(1));
            Assert.That(result.AverageRating, Is.EqualTo(5));
            Assert.That(result.Reviews[0].CustomerName, Is.EqualTo("Ahmed Ali"));
            _recentlyViewedService.Verify(s => s.RecordViewAsync(userId, product.ProductId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task EnsureGetProductDetailsHandlerSkipsViewRecordingWhenUserIsAnonymous()
        {
            _products.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Product, bool>>>(),
                    It.IsAny<Expression<Func<Product, object>>[]>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ValidProduct());
            _currentUserService.Setup(s => s.UserId).Returns((Guid?)null);
            _reviews.Setup(r => r.FindAllAsync(
                    It.IsAny<Expression<Func<Review, bool>>>(),
                    It.IsAny<Expression<Func<Review, object>>[]>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            var result = await _getDetailsHandler.Handle(new GetProductDetailsQuery(Guid.NewGuid()), It.IsAny<CancellationToken>());

            Assert.That(result.ReviewCount, Is.EqualTo(0));
            _recentlyViewedService.Verify(s => s.RecordViewAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task EnsureGetRecentlyViewedHandlerReturnsEmptyListWhenNoViewsRecorded()
        {
            var userId = Guid.NewGuid();
            _currentUserService.Setup(s => s.UserId).Returns(userId);
            _recentlyViewedService.Setup(s => s.GetRecentProductIdsAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            var result = await _getRecentlyViewedHandler.Handle(It.IsAny<GetRecentlyViewedQuery>(), It.IsAny<CancellationToken>());

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void EnsureGetRecentlyViewedHandlerThrowsNotFoundWhenUserNotResolved()
        {
            _currentUserService.Setup(s => s.UserId).Returns((Guid?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _getRecentlyViewedHandler.Handle(It.IsAny<GetRecentlyViewedQuery>(), It.IsAny<CancellationToken>()));
        }

        [Test]
        public async Task EnsureGetRecentlyViewedHandlerOrdersProductsByMostRecentView()
        {
            var product1 = ValidProduct();
            var product2 = ValidProduct();
            var userId = Guid.NewGuid();
            _currentUserService.Setup(s => s.UserId).Returns(userId);
            _recentlyViewedService.Setup(s => s.GetRecentProductIdsAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync([product2.ProductId, product1.ProductId]);
            _products.Setup(r => r.FindAllAsync(
                    It.IsAny<Expression<Func<Product, bool>>>(),
                    It.IsAny<Expression<Func<Product, object>>[]>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync([product1, product2]);

            var result = await _getRecentlyViewedHandler.Handle(It.IsAny<GetRecentlyViewedQuery>(), It.IsAny<CancellationToken>());

            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[0].ProductId, Is.EqualTo(product2.ProductId));
            Assert.That(result[1].ProductId, Is.EqualTo(product1.ProductId));
        }

        [Test]
        public async Task EnsureSearchProductsHandlerReturnsPageWhenProductsMatch()
        {
            var product = ValidProduct();
            _products.Setup(r => r.FindPagedAsync(
                    It.IsAny<Expression<Func<Product, bool>>>(),
                    It.Is<Expression<Func<Product, object>>[]>(includes => includes.Length == 2),
                    It.IsAny<SortDirection>(),
                    It.IsAny<Expression<Func<Product, object>>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(((IReadOnlyList<Product>)[product], 1));

            var result = await _searchHandler.Handle(new SearchProductsQuery(Search: "chair"), It.IsAny<CancellationToken>());

            Assert.That(result.TotalCount, Is.EqualTo(1));
            Assert.That(result.Items.Count, Is.EqualTo(1));
            Assert.That(result.Items[0].ProductName, Is.EqualTo("Wooden Chair"));
            Assert.That(result.Items[0].ReviewCount, Is.EqualTo(0));
        }

        [Test]
        public async Task EnsureCreateProductHandlerSavesProductWhenCommandIsValid()
        {
            _products.Setup(r => r.AddAsync(
                    It.IsAny<Product>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _unitOfWork.Setup(u => u.CompleteAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var result = await _createHandler.Handle(
                new CreateProductCommand
                {
                    ProductName = "Wooden Chair",
                    Description = "A comfortable wooden chair",
                    Price = 150m,
                    StockQuantity = 10,
                    CategoryId = Guid.NewGuid(),
                    ImageUrl = "https://example.com/chair.jpg",
                    IsActive = true
                },
                It.IsAny<CancellationToken>());

            Assert.That(result, Is.TypeOf<Guid>());
            _products.Verify(r => r.AddAsync(It.Is<Product>(p => p.ProductName == "Wooden Chair" && p.Price == 150m), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task EnsureUpdateProductHandlerReturnsTrueWhenProductExistsWithoutPriceChange()
        {
            _products.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(ValidProduct());
            _unitOfWork.Setup(u => u.CompleteAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var result = await _updateHandler.Handle(
                new UpdateProductCommand { ProductId = Guid.NewGuid(), StockQuantity = 20 },
                It.IsAny<CancellationToken>());

            Assert.That(result, Is.True);
            _products.Verify(r => r.Update(It.IsAny<Product>()), Times.Once);
            _priceDropDetection.Verify(j => j.ExecuteAsync(It.IsAny<List<Guid>>()), Times.Never);
        }

        [Test]
        public async Task EnsureUpdateProductHandlerNotifiesWishlistsWhenPriceDrops()
        {
            var product = ValidProduct();
            var watcherId = Guid.NewGuid();
            _products.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(product);
            _wishlistItems.Setup(r => r.FindAllAsync(
                    It.IsAny<Expression<Func<WishlistItem, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync([new WishlistItem { UserId = watcherId, ProductId = product.ProductId, ProductOriginalPrice = 200m }]);
            _unitOfWork.Setup(u => u.CompleteAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
            _priceDropDetection.Setup(j => j.ExecuteAsync(It.IsAny<List<Guid>>()))
                .Returns(Task.CompletedTask);

            var result = await _updateHandler.Handle(
                new UpdateProductCommand { ProductId = product.ProductId, Price = 120m },
                It.IsAny<CancellationToken>());

            Assert.That(result, Is.True);
            _priceDropDetection.Verify(j => j.ExecuteAsync(It.Is<List<Guid>>(ids => ids.Count == 1 && ids[0] == watcherId)), Times.Once);
        }

        [Test]
        public async Task EnsureUpdateProductHandlerReturnsFalseWhenProductNotExists()
        {
            _products.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Product?)null);

            var result = await _updateHandler.Handle(
                new UpdateProductCommand { ProductId = Guid.NewGuid(), Price = 120m },
                It.IsAny<CancellationToken>());

            Assert.That(result, Is.False);
            _products.Verify(r => r.Update(It.IsAny<Product>()), Times.Never);
        }

        [Test]
        public async Task EnsureDeleteProductHandlerDeletesProductWhenItHasNoOrderItems()
        {
            var product = ValidProduct();
            _products.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Product, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(product);
            _orderItems.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<OrderItem, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((OrderItem?)null);
            _unitOfWork.Setup(u => u.CompleteAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var result = await _deleteHandler.Handle(
                new DeleteProductCommand(product.ProductId),
                It.IsAny<CancellationToken>());

            Assert.That(result, Is.True);
            _products.Verify(r => r.Delete(product), Times.Once);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task EnsureDeleteProductHandlerThrowsConflictWhenProductHasBeenOrdered()
        {
            var product = ValidProduct();
            _products.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Product, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(product);
            _orderItems.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<OrderItem, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OrderItem { ProductId = product.ProductId });

            var exception = await Assert.ThrowsAsync<ConflictException>(async () =>
                await _deleteHandler.Handle(
                    new DeleteProductCommand(product.ProductId),
                    It.IsAny<CancellationToken>()));

            Assert.That(exception!.Message, Does.Contain("ordered"));
            _products.Verify(r => r.Delete(It.IsAny<Product>()), Times.Never);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task EnsureDeleteProductHandlerReturnsFalseWhenProductNotExists()
        {
            _products.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Product, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Product?)null);

            var result = await _deleteHandler.Handle(
                new DeleteProductCommand(Guid.NewGuid()),
                It.IsAny<CancellationToken>());

            Assert.That(result, Is.False);
            _products.Verify(r => r.Delete(It.IsAny<Product>()), Times.Never);
        }
    }
}
