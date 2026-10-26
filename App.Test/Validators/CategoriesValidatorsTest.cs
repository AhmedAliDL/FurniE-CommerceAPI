using App.Application.Categories.Command.CreateCategory;
using App.Application.Categories.Command.UpdateCategory;
using NUnit.Framework;

namespace App.Test.Validators
{
    [TestFixture]
    [Category("Categories")]
    public class CategoriesValidatorsTest
    {
        private CreateCategoryValidator _validator = null!;
        private UpdateCategoryValidator _updateValidator = null!;

        [SetUp]
        public void Setup()
        {
            _validator = new CreateCategoryValidator();
            _updateValidator = new UpdateCategoryValidator();
        }

        [Test]
        public void EnsureThatCreateCategoryCommandIsValidWhenNameProvided()
        {
            var command = new CreateCategoryCommand { CategoryName = "Chairs" };

            var result = _validator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatCreateCategoryCommandIsInvalidWhenNameMissing()
        {
            var command = new CreateCategoryCommand { CategoryName = "" };

            var result = _validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatCreateCategoryCommandIsValidWhenNameHasFiftyCharacters()
        {
            var command = new CreateCategoryCommand { CategoryName = new string('a', 50) };

            var result = _validator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatCreateCategoryCommandIsInvalidWhenNameTooLong()
        {
            var command = new CreateCategoryCommand { CategoryName = new string('a', 51) };

            var result = _validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatUpdateCategoryCommandIsValidWhenNameProvided()
        {
            var command = new UpdateCategoryCommand { CategoryId = Guid.NewGuid(), CategoryName = "Sofas" };

            var result = _updateValidator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatUpdateCategoryCommandIsValidWhenNameIsOmitted()
        {
            var command = new UpdateCategoryCommand { CategoryId = Guid.NewGuid() };

            var result = _updateValidator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatUpdateCategoryCommandIsInvalidWhenCategoryIdMissing()
        {
            var command = new UpdateCategoryCommand { CategoryName = "Sofas" };

            var result = _updateValidator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatUpdateCategoryCommandIsInvalidWhenNameIsEmpty()
        {
            var command = new UpdateCategoryCommand { CategoryId = Guid.NewGuid(), CategoryName = "" };

            var result = _updateValidator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatUpdateCategoryCommandIsInvalidWhenNameTooLong()
        {
            var command = new UpdateCategoryCommand
            {
                CategoryId = Guid.NewGuid(),
                CategoryName = new string('a', 51)
            };

            var result = _updateValidator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }
    }
}
