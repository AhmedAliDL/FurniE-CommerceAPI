using App.Application.Products.Command.CreateProduct;
using App.Application.Products.Command.ReorderProductImages;
using App.Application.Products.Command.UpdateProduct;
using App.Application.Products.Command.UploadProductImages;
using App.Application.Products.Dto;
using App.Application.Products.Queries.GetActiveProducts;
using App.Application.Products.Queries.GetAllProducts;
using App.Application.Products.Queries.GetProductsByCategory;
using App.Application.Products.Queries.SearchProducts;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace App.Test.Validators
{
    [TestFixture]
    [Category("Products")]
    public class ProductsValidatorsTest
    {
        private CreateProductValidator _createValidator = null!;
        private UpdateProductValidator _updateValidator = null!;
        private ReorderProductImagesValidator _reorderValidator = null!;
        private UploadProductImagesValidator _uploadValidator = null!;

        [SetUp]
        public void Setup()
        {
            _createValidator = new CreateProductValidator();
            _updateValidator = new UpdateProductValidator();
            _reorderValidator = new ReorderProductImagesValidator();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FileStorage:AllowedExtensions"] = ".jpg,.jpeg,.png,.webp",
                    ["FileStorage:MaxFileSizeBytes"] = "5242880",
                    ["FileStorage:MaxFilesPerUpload"] = "10"
                })
                .Build();
            _uploadValidator = new UploadProductImagesValidator(configuration);
        }

        private static CreateProductCommand ValidCreateCommand() => new()
        {
            ProductName = "Wooden Chair",
            Description = "A comfortable wooden chair",
            Price = 150m,
            StockQuantity = 10,
            CategoryId = Guid.NewGuid(),
            ImageUrl = "https://example.com/chair.jpg"
        };

        [Test]
        public void EnsureThatCreateProductCommandIsValidWhenAllFieldsProvided()
        {
            var result = _createValidator.Validate(ValidCreateCommand());

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatCreateProductCommandIsInvalidWhenNameMissing()
        {
            var command = ValidCreateCommand();
            command = command with { ProductName = "" };

            var result = _createValidator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatCreateProductCommandIsInvalidWhenNameExceedsMaxLength()
        {
            var command = ValidCreateCommand();
            command = command with { ProductName = new string('a', 51) };

            var result = _createValidator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatCreateProductCommandIsInvalidWhenDescriptionTooShort()
        {
            var command = ValidCreateCommand();
            command = command with { Description = "short" };

            var result = _createValidator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatCreateProductCommandIsInvalidWhenPriceIsNegative()
        {
            var command = ValidCreateCommand();
            command = command with { Price = -1m };

            var result = _createValidator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatCreateProductCommandIsInvalidWhenStockIsNegative()
        {
            var command = ValidCreateCommand();
            command = command with { StockQuantity = -1 };

            var result = _createValidator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatCreateProductCommandIsInvalidWhenImageUrlIsNotAbsolute()
        {
            var command = ValidCreateCommand();
            command = command with { ImageUrl = "not-a-url" };

            var result = _createValidator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatUpdateProductCommandIsValidWhenOnlyProductIdProvided()
        {
            var command = new UpdateProductCommand { ProductId = Guid.NewGuid() };

            var result = _updateValidator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatUpdateProductCommandIsInvalidWhenProductIdMissing()
        {
            var command = new UpdateProductCommand { Price = 100m };

            var result = _updateValidator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatUpdateProductCommandIsInvalidWhenNameExceedsMaxLength()
        {
            var command = new UpdateProductCommand { ProductId = Guid.NewGuid(), ProductName = new string('a', 51) };

            var result = _updateValidator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatUpdateProductCommandIsInvalidWhenPriceIsNegative()
        {
            var command = new UpdateProductCommand { ProductId = Guid.NewGuid(), Price = -1m };

            var result = _updateValidator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatReorderProductImagesCommandIsValidWhenImageIdsAreUnique()
        {
            var command = new ReorderProductImagesCommand(Guid.NewGuid(), [Guid.NewGuid(), Guid.NewGuid()]);

            var result = _reorderValidator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatReorderProductImagesCommandIsInvalidWhenImageIdsContainDuplicates()
        {
            var imageId = Guid.NewGuid();
            var command = new ReorderProductImagesCommand(Guid.NewGuid(), [imageId, imageId]);

            var result = _reorderValidator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatReorderProductImagesCommandIsInvalidWhenImageIdsMissing()
        {
            var command = new ReorderProductImagesCommand(Guid.NewGuid(), []);

            var result = _reorderValidator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatUploadProductImagesCommandIsValidWhenFilesAreValid()
        {
            var command = new UploadProductImagesCommand
            {
                ProductId = Guid.NewGuid(),
                Files = [new ImageFileUpload("chair.jpg", "image/jpeg", 5, new MemoryStream([1, 2, 3, 4, 5]))]
            };

            var result = _uploadValidator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatUploadProductImagesCommandIsInvalidWhenProductIdMissing()
        {
            var command = new UploadProductImagesCommand
            {
                Files = [new ImageFileUpload("chair.jpg", "image/jpeg", 5, new MemoryStream([1, 2, 3, 4, 5]))]
            };

            var result = _uploadValidator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatUploadProductImagesCommandIsInvalidWhenFilesMissing()
        {
            var command = new UploadProductImagesCommand { ProductId = Guid.NewGuid() };

            var result = _uploadValidator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatUploadProductImagesCommandIsInvalidWhenExtensionNotAllowed()
        {
            var command = new UploadProductImagesCommand
            {
                ProductId = Guid.NewGuid(),
                Files = [new ImageFileUpload("chair.gif", "image/gif", 5, new MemoryStream([1, 2, 3, 4, 5]))]
            };

            var result = _uploadValidator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatUploadProductImagesCommandIsInvalidWhenFileIsEmpty()
        {
            var command = new UploadProductImagesCommand
            {
                ProductId = Guid.NewGuid(),
                Files = [new ImageFileUpload("chair.jpg", "image/jpeg", 0, new MemoryStream())]
            };

            var result = _uploadValidator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatGetProductsByCategoryQueryIsValidWhenCategoryNameProvided()
        {
            var validator = new GetProductsByCategoryValidator();
            var query = new GetProductsByCategoryQuery("Chairs");

            var result = validator.Validate(query);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatGetProductsByCategoryQueryIsInvalidWhenCategoryNameMissing()
        {
            var validator = new GetProductsByCategoryValidator();
            var query = new GetProductsByCategoryQuery("");

            var result = validator.Validate(query);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatGetProductsByCategoryQueryIsInvalidWhenCategoryNameTooLong()
        {
            var validator = new GetProductsByCategoryValidator();
            var query = new GetProductsByCategoryQuery(new string('a', 101));

            var result = validator.Validate(query);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatSearchProductsQueryIsValidWithDefaultValues()
        {
            var validator = new SearchProductsValidator();
            var query = new SearchProductsQuery();

            var result = validator.Validate(query);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatSearchProductsQueryIsInvalidWhenPriceRangeIsInverted()
        {
            var validator = new SearchProductsValidator();
            var query = new SearchProductsQuery(MinPrice: 500m, MaxPrice: 100m);

            var result = validator.Validate(query);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatSearchProductsQueryIsInvalidWhenMinPriceIsNegative()
        {
            var validator = new SearchProductsValidator();
            var query = new SearchProductsQuery(MinPrice: -1m);

            var result = validator.Validate(query);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatSearchProductsQueryIsInvalidWhenMinRatingOutOfRange()
        {
            var validator = new SearchProductsValidator();
            var query = new SearchProductsQuery(MinRating: 6);

            var result = validator.Validate(query);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatSearchProductsQueryIsInvalidWhenPageIsZero()
        {
            var validator = new SearchProductsValidator();
            var query = new SearchProductsQuery(Page: 0);

            var result = validator.Validate(query);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatSearchProductsQueryIsInvalidWhenPageSizeExceedsMaximum()
        {
            var validator = new SearchProductsValidator();
            var query = new SearchProductsQuery(PageSize: 101);

            var result = validator.Validate(query);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatGetAllProductsQueryIsValidWithDefaultValues()
        {
            var validator = new GetAllProductsValidator();
            var query = new GetAllProductsQuery();

            var result = validator.Validate(query);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatGetAllProductsQueryIsInvalidWhenPageIsZero()
        {
            var validator = new GetAllProductsValidator();
            var query = new GetAllProductsQuery(Page: 0);

            var result = validator.Validate(query);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatGetAllProductsQueryIsInvalidWhenPageSizeExceedsMaximum()
        {
            var validator = new GetAllProductsValidator();
            var query = new GetAllProductsQuery(PageSize: 101);

            var result = validator.Validate(query);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatGetActiveProductsQueryIsValidWithDefaultValues()
        {
            var validator = new GetActiveProductsValidator();
            var query = new GetActiveProductsQuery();

            var result = validator.Validate(query);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatGetActiveProductsQueryIsInvalidWhenPageIsZero()
        {
            var validator = new GetActiveProductsValidator();
            var query = new GetActiveProductsQuery(Page: 0);

            var result = validator.Validate(query);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatGetActiveProductsQueryIsInvalidWhenPageSizeExceedsMaximum()
        {
            var validator = new GetActiveProductsValidator();
            var query = new GetActiveProductsQuery(PageSize: 101);

            var result = validator.Validate(query);

            Assert.That(result.IsValid, Is.False);
        }
    }
}
