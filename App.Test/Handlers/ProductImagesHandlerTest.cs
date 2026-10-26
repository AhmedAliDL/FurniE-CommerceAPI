using System.Linq.Expressions;
using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Repos;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Products.Command.DeleteProductImage;
using App.Application.Products.Command.ReorderProductImages;
using App.Application.Products.Command.SetPrimaryProductImage;
using App.Application.Products.Command.UploadProductImages;
using App.Application.Products.Dto;
using App.Application.Products.Queries.GetProductImages;
using App.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace App.Test.Handlers
{
    [TestFixture]
    [Category("ProductImages")]
    public class ProductImagesHandlerTest
    {
        private Mock<IUnitOfWork> _unitOfWork = null!;
        private Mock<IBaseRepository<Product>> _products = null!;
        private Mock<IBaseRepository<ProductImage>> _productImages = null!;
        private Mock<IFileStorageService> _fileStorageService = null!;

        private GetProductImagesHandler _getProductImagesHandler = null!;
        private UploadProductImagesHandler _uploadProductImagesHandler = null!;
        private SetPrimaryProductImageHandler _setPrimaryProductImageHandler = null!;
        private ReorderProductImagesHandler _reorderProductImagesHandler = null!;
        private DeleteProductImageHandler _deleteProductImageHandler = null!;

        private readonly Guid _productId = Guid.NewGuid();

        [SetUp]
        public void Setup()
        {
            _unitOfWork = new Mock<IUnitOfWork>();
            _products = new Mock<IBaseRepository<Product>>();
            _productImages = new Mock<IBaseRepository<ProductImage>>();
            _fileStorageService = new Mock<IFileStorageService>();

            _unitOfWork.Setup(u => u.Products).Returns(_products.Object);
            _unitOfWork.Setup(u => u.ProductImages).Returns(_productImages.Object);

            _getProductImagesHandler = new GetProductImagesHandler(_unitOfWork.Object, new Mock<ILogger<GetProductImagesHandler>>().Object);
            _uploadProductImagesHandler = new UploadProductImagesHandler(_unitOfWork.Object, _fileStorageService.Object, new Mock<ILogger<UploadProductImagesHandler>>().Object);
            _setPrimaryProductImageHandler = new SetPrimaryProductImageHandler(_unitOfWork.Object, new Mock<ILogger<SetPrimaryProductImageHandler>>().Object);
            _reorderProductImagesHandler = new ReorderProductImagesHandler(_unitOfWork.Object, new Mock<ILogger<ReorderProductImagesHandler>>().Object);
            _deleteProductImageHandler = new DeleteProductImageHandler(_unitOfWork.Object, _fileStorageService.Object, new Mock<ILogger<DeleteProductImageHandler>>().Object);
        }

        private void SetupProduct(string imageUrl = "old.jpg")
        {
            _products.Setup(p => p.FindAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Product { ProductId = _productId, ProductName = "Sofa", ImageUrl = imageUrl });
        }

        [Test]
        public async Task EnsureGetProductImagesHandlerReturnsImagesWithPrimaryFirstWhenProductExists()
        {
            SetupProduct();
            _productImages.Setup(pi => pi.FindAllAsync(It.IsAny<Expression<Func<ProductImage, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ProductImage>
                {
                    new() { Id = Guid.NewGuid(), ProductId = _productId, ImageUrl = "second.jpg", IsPrimary = false, SortOrder = 0 },
                    new() { Id = Guid.NewGuid(), ProductId = _productId, ImageUrl = "first.jpg", IsPrimary = true, SortOrder = 1 }
                });

            var result = await _getProductImagesHandler.Handle(new GetProductImagesQuery(_productId), It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.True);
                Assert.That(result.Value, Has.Count.EqualTo(2));
                Assert.That(result.Value[0].IsPrimary, Is.True);
                Assert.That(result.Value[0].ImageUrl, Is.EqualTo("first.jpg"));
                Assert.That(result.Value[1].IsPrimary, Is.False);
                Assert.That(result.Value[1].SortOrder, Is.EqualTo(0));
            });
        }

        [Test]
        public void EnsureGetProductImagesHandlerThrowsNotFoundWhenProductNotFound()
        {
            _products.Setup(p => p.FindAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Product?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _getProductImagesHandler.Handle(new GetProductImagesQuery(_productId), It.IsAny<CancellationToken>()));
        }

        [Test]
        public async Task EnsureUploadProductImagesHandlerSetsFirstImageAsPrimaryWhenProductHasNoImages()
        {
            SetupProduct();
            _productImages.Setup(pi => pi.FindAllAsync(It.IsAny<Expression<Func<ProductImage, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ProductImage>());
            _fileStorageService.Setup(f => f.SaveAsync("a.jpg", It.IsAny<Stream>(), It.IsAny<CancellationToken>())).ReturnsAsync("https://cdn.furni.com/a.jpg");
            _fileStorageService.Setup(f => f.SaveAsync("b.jpg", It.IsAny<Stream>(), It.IsAny<CancellationToken>())).ReturnsAsync("https://cdn.furni.com/b.jpg");

            var request = new UploadProductImagesCommand
            {
                ProductId = _productId,
                Files =
                [
                    new ImageFileUpload("a.jpg", "image/jpeg", 10, new MemoryStream([1, 2, 3])),
                    new ImageFileUpload("b.jpg", "image/jpeg", 10, new MemoryStream([4, 5, 6]))
                ]
            };

            var result = await _uploadProductImagesHandler.Handle(request, It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.True);
                Assert.That(result.Value, Has.Count.EqualTo(2));
                Assert.That(result.Value[0].IsPrimary, Is.True);
                Assert.That(result.Value[0].ImageUrl, Is.EqualTo("https://cdn.furni.com/a.jpg"));
                Assert.That(result.Value[1].IsPrimary, Is.False);
            });
            _productImages.Verify(pi => pi.AddRangeAsync(It.Is<List<ProductImage>>(images =>
                images.Count == 2 &&
                images[0].IsPrimary &&
                !images[1].IsPrimary &&
                images[0].SortOrder == 0 &&
                images[1].SortOrder == 1), It.IsAny<CancellationToken>()), Times.Once);
            _products.Verify(p => p.Update(It.Is<Product>(prod => prod.ImageUrl == "https://cdn.furni.com/a.jpg")), Times.Once);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task EnsureUploadProductImagesHandlerKeepsExistingPrimaryWhenProductHasImages()
        {
            SetupProduct();
            _productImages.Setup(pi => pi.FindAllAsync(It.IsAny<Expression<Func<ProductImage, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ProductImage>
                {
                    new() { Id = Guid.NewGuid(), ProductId = _productId, ImageUrl = "existing.jpg", IsPrimary = true, SortOrder = 0 }
                });
            _fileStorageService.Setup(f => f.SaveAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>())).ReturnsAsync("https://cdn.furni.com/new.jpg");

            var request = new UploadProductImagesCommand
            {
                ProductId = _productId,
                Files = [new ImageFileUpload("new.jpg", "image/jpeg", 10, new MemoryStream([1]))]
            };

            var result = await _uploadProductImagesHandler.Handle(request, It.IsAny<CancellationToken>());

            Assert.That(result.IsSuccess, Is.True);
            _productImages.Verify(pi => pi.AddRangeAsync(It.Is<List<ProductImage>>(images =>
                images.Count == 1 &&
                !images[0].IsPrimary), It.IsAny<CancellationToken>()), Times.Once);
            _products.Verify(p => p.Update(It.IsAny<Product>()), Times.Never);
        }

        [Test]
        public void EnsureUploadProductImagesHandlerDeletesStoredFilesWhenSaveFails()
        {
            SetupProduct();
            _productImages.Setup(pi => pi.FindAllAsync(It.IsAny<Expression<Func<ProductImage, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ProductImage>());
            _fileStorageService.Setup(f => f.SaveAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>())).ReturnsAsync("https://cdn.furni.com/a.jpg");
            _unitOfWork.Setup(u => u.CompleteAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("upload failed"));

            var request = new UploadProductImagesCommand
            {
                ProductId = _productId,
                Files = [new ImageFileUpload("a.jpg", "image/jpeg", 10, new MemoryStream([1]))]
            };

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _uploadProductImagesHandler.Handle(request, It.IsAny<CancellationToken>()));
            _fileStorageService.Verify(f => f.DeleteAsync("https://cdn.furni.com/a.jpg", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void EnsureUploadProductImagesHandlerThrowsNotFoundWhenProductNotFound()
        {
            _products.Setup(p => p.FindAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Product?)null);

            var request = new UploadProductImagesCommand { ProductId = _productId, Files = [] };

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _uploadProductImagesHandler.Handle(request, It.IsAny<CancellationToken>()));
        }

        [Test]
        public async Task EnsureSetPrimaryProductImageHandlerDemotesOldPrimaryAndPromotesTarget()
        {
            SetupProduct();
            var target = new ProductImage { Id = Guid.NewGuid(), ProductId = _productId, ImageUrl = "target.jpg", IsPrimary = false, SortOrder = 1 };
            var oldPrimary = new ProductImage { Id = Guid.NewGuid(), ProductId = _productId, ImageUrl = "old.jpg", IsPrimary = true, SortOrder = 0 };
            _productImages.Setup(pi => pi.FindAsync(It.IsAny<Expression<Func<ProductImage, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(target);
            _productImages.Setup(pi => pi.FindAllAsync(It.IsAny<Expression<Func<ProductImage, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ProductImage> { oldPrimary });

            var result = await _setPrimaryProductImageHandler.Handle(new SetPrimaryProductImageCommand(_productId, target.Id), It.IsAny<CancellationToken>());

            Assert.That(result.IsSuccess, Is.True);
            _productImages.Verify(pi => pi.Update(It.Is<ProductImage>(i => i.Id == oldPrimary.Id && !i.IsPrimary)), Times.Once);
            _productImages.Verify(pi => pi.Update(It.Is<ProductImage>(i => i.Id == target.Id && i.IsPrimary)), Times.Once);
            _products.Verify(p => p.Update(It.Is<Product>(prod => prod.ImageUrl == "target.jpg")), Times.Once);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Test]
        public async Task EnsureSetPrimaryProductImageHandlerSkipsUpdateWhenTargetIsAlreadyPrimary()
        {
            SetupProduct();
            var target = new ProductImage { Id = Guid.NewGuid(), ProductId = _productId, ImageUrl = "old.jpg", IsPrimary = true, SortOrder = 0 };
            _productImages.Setup(pi => pi.FindAsync(It.IsAny<Expression<Func<ProductImage, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(target);

            var result = await _setPrimaryProductImageHandler.Handle(new SetPrimaryProductImageCommand(_productId, target.Id), It.IsAny<CancellationToken>());

            Assert.That(result.IsSuccess, Is.True);
            _productImages.Verify(pi => pi.Update(It.IsAny<ProductImage>()), Times.Never);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public void EnsureSetPrimaryProductImageHandlerThrowsNotFoundWhenImageNotFound()
        {
            SetupProduct();
            _productImages.Setup(pi => pi.FindAsync(It.IsAny<Expression<Func<ProductImage, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ProductImage?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _setPrimaryProductImageHandler.Handle(new SetPrimaryProductImageCommand(_productId, Guid.NewGuid()), It.IsAny<CancellationToken>()));
        }

        [Test]
        public async Task EnsureReorderProductImagesHandlerUpdatesSortOrdersWhenListMatches()
        {
            SetupProduct();
            var imageA = new ProductImage { Id = Guid.NewGuid(), ProductId = _productId, ImageUrl = "a.jpg", SortOrder = 0 };
            var imageB = new ProductImage { Id = Guid.NewGuid(), ProductId = _productId, ImageUrl = "b.jpg", SortOrder = 1 };
            _productImages.Setup(pi => pi.FindAllAsync(It.IsAny<Expression<Func<ProductImage, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ProductImage> { imageA, imageB });

            var result = await _reorderProductImagesHandler.Handle(new ReorderProductImagesCommand(_productId, [imageB.Id, imageA.Id]), It.IsAny<CancellationToken>());

            Assert.That(result.IsSuccess, Is.True);
            _productImages.Verify(pi => pi.Update(It.Is<ProductImage>(i => i.Id == imageB.Id && i.SortOrder == 0)), Times.Once);
            _productImages.Verify(pi => pi.Update(It.Is<ProductImage>(i => i.Id == imageA.Id && i.SortOrder == 1)), Times.Once);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task EnsureReorderProductImagesHandlerSkipsUpdateWhenOrderIsUnchanged()
        {
            SetupProduct();
            var imageA = new ProductImage { Id = Guid.NewGuid(), ProductId = _productId, ImageUrl = "a.jpg", SortOrder = 0 };
            var imageB = new ProductImage { Id = Guid.NewGuid(), ProductId = _productId, ImageUrl = "b.jpg", SortOrder = 1 };
            _productImages.Setup(pi => pi.FindAllAsync(It.IsAny<Expression<Func<ProductImage, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ProductImage> { imageA, imageB });

            var result = await _reorderProductImagesHandler.Handle(new ReorderProductImagesCommand(_productId, [imageA.Id, imageB.Id]), It.IsAny<CancellationToken>());

            Assert.That(result.IsSuccess, Is.True);
            _productImages.Verify(pi => pi.Update(It.IsAny<ProductImage>()), Times.Never);
        }

        [Test]
        public async Task EnsureReorderProductImagesHandlerReturnsFailureWhenListDoesNotMatch()
        {
            SetupProduct();
            var imageA = new ProductImage { Id = Guid.NewGuid(), ProductId = _productId, ImageUrl = "a.jpg", SortOrder = 0 };
            _productImages.Setup(pi => pi.FindAllAsync(It.IsAny<Expression<Func<ProductImage, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ProductImage> { imageA });

            var result = await _reorderProductImagesHandler.Handle(new ReorderProductImagesCommand(_productId, [Guid.NewGuid()]), It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Errors, Has.Some.EqualTo("The image id list must contain exactly this product's images."));
            });
            _productImages.Verify(pi => pi.Update(It.IsAny<ProductImage>()), Times.Never);
        }

        [Test]
        public void EnsureReorderProductImagesHandlerThrowsNotFoundWhenProductNotFound()
        {
            _products.Setup(p => p.FindAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Product?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _reorderProductImagesHandler.Handle(new ReorderProductImagesCommand(_productId, []), It.IsAny<CancellationToken>()));
        }

        [Test]
        public async Task EnsureDeleteProductImageHandlerPromotesNextImageWhenPrimaryIsDeleted()
        {
            SetupProduct();
            var primary = new ProductImage { Id = Guid.NewGuid(), ProductId = _productId, ImageUrl = "primary.jpg", IsPrimary = true, SortOrder = 0 };
            var next = new ProductImage { Id = Guid.NewGuid(), ProductId = _productId, ImageUrl = "next.jpg", IsPrimary = false, SortOrder = 1 };
            _productImages.Setup(pi => pi.FindAsync(It.IsAny<Expression<Func<ProductImage, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(primary);
            _productImages.Setup(pi => pi.FindAllAsync(It.IsAny<Expression<Func<ProductImage, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ProductImage> { next });

            var result = await _deleteProductImageHandler.Handle(new DeleteProductImageCommand(_productId, primary.Id), It.IsAny<CancellationToken>());

            Assert.That(result.IsSuccess, Is.True);
            _productImages.Verify(pi => pi.Delete(primary), Times.Once);
            _productImages.Verify(pi => pi.Update(It.Is<ProductImage>(i => i.Id == next.Id && i.IsPrimary)), Times.Once);
            _products.Verify(p => p.Update(It.Is<Product>(prod => prod.ImageUrl == "next.jpg")), Times.Once);
            _fileStorageService.Verify(f => f.DeleteAsync("primary.jpg", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task EnsureDeleteProductImageHandlerSkipsPromotionWhenImageIsNotPrimary()
        {
            SetupProduct();
            var image = new ProductImage { Id = Guid.NewGuid(), ProductId = _productId, ImageUrl = "secondary.jpg", IsPrimary = false, SortOrder = 1 };
            _productImages.Setup(pi => pi.FindAsync(It.IsAny<Expression<Func<ProductImage, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(image);

            var result = await _deleteProductImageHandler.Handle(new DeleteProductImageCommand(_productId, image.Id), It.IsAny<CancellationToken>());

            Assert.That(result.IsSuccess, Is.True);
            _productImages.Verify(pi => pi.Update(It.IsAny<ProductImage>()), Times.Never);
            _products.Verify(p => p.Update(It.IsAny<Product>()), Times.Never);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
            _fileStorageService.Verify(f => f.DeleteAsync("secondary.jpg", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void EnsureDeleteProductImageHandlerThrowsNotFoundWhenImageNotFound()
        {
            SetupProduct();
            _productImages.Setup(pi => pi.FindAsync(It.IsAny<Expression<Func<ProductImage, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ProductImage?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _deleteProductImageHandler.Handle(new DeleteProductImageCommand(_productId, Guid.NewGuid()), It.IsAny<CancellationToken>()));
        }
    }
}
