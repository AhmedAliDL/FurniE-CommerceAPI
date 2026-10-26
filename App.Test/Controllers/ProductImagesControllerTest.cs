using App.Application.Products.Command.DeleteProductImage;
using App.Application.Products.Command.ReorderProductImages;
using App.Application.Products.Command.SetPrimaryProductImage;
using App.Application.Products.Command.UploadProductImages;
using App.Application.Products.Dto;
using App.Application.Products.Queries.GetProductImages;
using App.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using App.Api.Controllers;

namespace App.Test.Controllers
{
    [TestFixture]
    [Category("ProductImages")]
    public class ProductImagesControllerTest
    {
        private Mock<IMediator> _sender = null!;
        private ProductImagesController _controller = null!;

        [SetUp]
        public void Setup()
        {
            _sender = new Mock<IMediator>();
            _controller = new ProductImagesController(_sender.Object);
        }

        [Test]
        public async Task EnsureThatGetImagesReturnsOkWhenProductExists()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<GetProductImagesQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResponseResult<List<ProductImageDto>>.Success([]));

            var result = await _controller.GetImages(Guid.NewGuid());

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatUploadReturnsOkWhenImagesAreUploaded()
        {
            using var stream = new MemoryStream("fake image"u8.ToArray());
            var file = new FormFile(stream, 0, stream.Length, "files", "chair.jpg")
            {
                Headers = new HeaderDictionary { ["Content-Type"] = "image/jpeg" }
            };
            var files = new FormFileCollection { file };
            _sender.Setup(s => s.Send(
                    It.IsAny<UploadProductImagesCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResponseResult<List<ProductImageDto>>.Success([]));

            var result = await _controller.Upload(Guid.NewGuid(), files);

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatUploadReturnsOkWhenNoFilesProvided()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<UploadProductImagesCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResponseResult<List<ProductImageDto>>.Success([]));

            var result = await _controller.Upload(Guid.NewGuid(), null);

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatUploadReturnsBadRequestWhenUploadFails()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<UploadProductImagesCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResponseResult<List<ProductImageDto>>.Failure("Only image files are allowed."));

            var result = await _controller.Upload(Guid.NewGuid(), null);

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task EnsureThatSetPrimaryReturnsNoContentWhenImagePromoted()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<SetPrimaryProductImageCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResponseResult.Success());

            var result = await _controller.SetPrimary(Guid.NewGuid(), Guid.NewGuid());

            Assert.That(result, Is.TypeOf<NoContentResult>());
        }

        [Test]
        public async Task EnsureThatSetPrimaryReturnsBadRequestWhenPromotionFails()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<SetPrimaryProductImageCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResponseResult.Failure("Image does not belong to the product."));

            var result = await _controller.SetPrimary(Guid.NewGuid(), Guid.NewGuid());

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task EnsureThatDeleteReturnsNoContentWhenImageDeleted()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<DeleteProductImageCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResponseResult.Success());

            var result = await _controller.Delete(Guid.NewGuid(), Guid.NewGuid());

            Assert.That(result, Is.TypeOf<NoContentResult>());
        }

        [Test]
        public async Task EnsureThatDeleteReturnsBadRequestWhenDeletionFails()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<DeleteProductImageCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResponseResult.Failure("Image does not belong to the product."));

            var result = await _controller.Delete(Guid.NewGuid(), Guid.NewGuid());

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task EnsureThatReorderReturnsNoContentWhenOrderUpdated()
        {
            var productId = Guid.NewGuid();
            var command = new ReorderProductImagesCommand(productId, [Guid.NewGuid()]);
            _sender.Setup(s => s.Send(
                    It.IsAny<ReorderProductImagesCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResponseResult.Success());

            var result = await _controller.Reorder(productId, command);

            Assert.That(result, Is.TypeOf<NoContentResult>());
        }

        [Test]
        public async Task EnsureThatReorderReturnsBadRequestWhenOrderInvalid()
        {
            var productId = Guid.NewGuid();
            var command = new ReorderProductImagesCommand(productId, [Guid.NewGuid()]);
            _sender.Setup(s => s.Send(
                    It.IsAny<ReorderProductImagesCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResponseResult.Failure("The list must contain every image id exactly once."));

            var result = await _controller.Reorder(productId, command);

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task EnsureThatReorderReturnsBadRequestWhenRouteIdMismatch()
        {
            var command = new ReorderProductImagesCommand(Guid.NewGuid(), [Guid.NewGuid()]);

            var result = await _controller.Reorder(Guid.NewGuid(), command);

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
            _sender.Verify(s => s.Send(It.IsAny<ReorderProductImagesCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
