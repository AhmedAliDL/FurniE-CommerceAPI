using App.Application.Categories.Dto;
using App.Application.Categories.Queries.GetAllCategories;
using App.Application.Products.Command.CreateProduct;
using App.Application.Products.Command.DeleteProduct;
using App.Application.Products.Command.UpdateProduct;
using App.Application.Products.Dto;
using App.Application.Products.Queries.GetActiveProducts;
using App.Application.Products.Queries.GetAllProducts;
using App.Application.Products.Queries.GetProductById;
using App.Application.Products.Queries.GetProductDetails;
using App.Application.Products.Queries.GetProductsByCategory;
using App.Application.Products.Queries.GetRecentlyViewed;
using App.Application.Products.Queries.SearchProducts;
using App.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using App.Api.Controllers;

namespace App.Test.Controllers
{
    [TestFixture]
    [Category("Products")]
    public class ProductControllerTest
    {
        private Mock<IMediator> _sender = null!;
        private ProductController _controller = null!;

        [SetUp]
        public void Setup()
        {
            _sender = new Mock<IMediator>();
            _controller = new ProductController(_sender.Object);
        }

        [Test]
        public async Task EnsureThatGetAllReturnsOkWhenProductsExist()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<GetAllProductsQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedResult<ProductResponse> { Page = 1, PageSize = 20, TotalCount = 0 });

            var result = await _controller.GetAll();

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatSearchReturnsOkWhenProductsMatch()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<SearchProductsQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedResult<ProductListItemDto> { Items = [], Page = 1, PageSize = 10, TotalCount = 0 });

            var result = await _controller.Search(new SearchProductsQuery(Search: "chair"));

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatGetActiveReturnsOkWhenProductsExist()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<GetActiveProductsQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedResult<ProductResponse> { Page = 1, PageSize = 20, TotalCount = 0 });
            _sender.Setup(s => s.Send(
                    It.IsAny<GetAllCategoriesQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<CategoryDto>());

            var result = await _controller.GetActive();

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatGetByCategoryReturnsOkWhenCategoryHasProducts()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<GetProductsByCategoryQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedResult<ProductResponse> { Page = 1, PageSize = 20, TotalCount = 0 });
            _sender.Setup(s => s.Send(
                    It.IsAny<GetAllCategoriesQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<CategoryDto>());

            var result = await _controller.GetByCategory("Chairs");

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatGetDetailsReturnsOkWhenProductExists()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<GetProductDetailsQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ProductDetailsDto());

            var result = await _controller.GetDetails(Guid.NewGuid());

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatGetByIdReturnsOkWhenProductExists()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<GetProductByIdQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ProductResponse());

            var result = await _controller.GetById(Guid.NewGuid());

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatGetRecentlyViewedReturnsOkWhenHistoryExists()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<GetRecentlyViewedQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ProductResponse>());

            var result = await _controller.GetRecentlyViewed();

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatCreateReturnsCreatedWhenProductIsValid()
        {
            var command = new CreateProductCommand
            {
                ProductName = "Chair",
                Description = "A wooden chair",
                Price = 150m,
                StockQuantity = 10,
                CategoryId = Guid.NewGuid(),
                ImageUrl = "https://example.com/chair.jpg"
            };
            _sender.Setup(s => s.Send(
                    It.IsAny<CreateProductCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Guid.NewGuid());

            var result = await _controller.Create(command);

            Assert.That(result, Is.TypeOf<CreatedAtActionResult>());
        }

        [Test]
        public async Task EnsureThatUpdateReturnsNoContentWhenProductUpdated()
        {
            var productId = Guid.NewGuid();
            var command = new UpdateProductCommand { ProductId = productId, Price = 120m };
            _sender.Setup(s => s.Send(
                    It.IsAny<UpdateProductCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var result = await _controller.Update(productId, command);

            Assert.That(result, Is.TypeOf<NoContentResult>());
        }

        [Test]
        public async Task EnsureThatUpdateReturnsNotFoundWhenProductNotExists()
        {
            var productId = Guid.NewGuid();
            var command = new UpdateProductCommand { ProductId = productId, Price = 120m };
            _sender.Setup(s => s.Send(
                    It.IsAny<UpdateProductCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var result = await _controller.Update(productId, command);

            Assert.That(result, Is.TypeOf<NotFoundResult>());
        }

        [Test]
        public async Task EnsureThatUpdateReturnsBadRequestWhenRouteIdMismatch()
        {
            var command = new UpdateProductCommand { ProductId = Guid.NewGuid(), Price = 120m };

            var result = await _controller.Update(Guid.NewGuid(), command);

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
            _sender.Verify(s => s.Send(It.IsAny<UpdateProductCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task EnsureThatDeleteReturnsNoContentWhenProductDeleted()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<DeleteProductCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var result = await _controller.Delete(Guid.NewGuid());

            Assert.That(result, Is.TypeOf<NoContentResult>());
        }

        [Test]
        public async Task EnsureThatDeleteReturnsNotFoundWhenProductDoesNotExist()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<DeleteProductCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var result = await _controller.Delete(Guid.NewGuid());

            Assert.That(result, Is.TypeOf<NotFoundResult>());
        }
    }
}
