using App.Application.Categories.Command.CreateCategory;
using App.Application.Categories.Command.DeleteCategory;
using App.Application.Categories.Command.UpdateCategory;
using App.Application.Categories.Queries.GetAllCategories;
using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Repos;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Linq.Expressions;

namespace App.Test.Handlers
{
    [TestFixture]
    [Category("Categories")]
    public class CategoriesHandlerTest
    {
        private Mock<IUnitOfWork> _unitOfWork = null!;
        private Mock<IBaseRepository<Category>> _categories = null!;
        private Mock<IBaseRepository<Product>> _products = null!;
        private Mock<ILogger<GetAllCategoriesHandler>> _getAllLogger = null!;
        private Mock<ILogger<CreateCategoryHandler>> _createLogger = null!;
        private Mock<ILogger<UpdateCategoryHandler>> _updateLogger = null!;
        private Mock<ILogger<DeleteCategoryHandler>> _deleteLogger = null!;
        private GetAllCategoriesHandler _getAllHandler = null!;
        private CreateCategoryHandler _createHandler = null!;
        private UpdateCategoryHandler _updateHandler = null!;
        private DeleteCategoryHandler _deleteHandler = null!;

        [SetUp]
        public void Setup()
        {
            _unitOfWork = new Mock<IUnitOfWork>();
            _categories = new Mock<IBaseRepository<Category>>();
            _products = new Mock<IBaseRepository<Product>>();
            _getAllLogger = new Mock<ILogger<GetAllCategoriesHandler>>();
            _createLogger = new Mock<ILogger<CreateCategoryHandler>>();
            _updateLogger = new Mock<ILogger<UpdateCategoryHandler>>();
            _deleteLogger = new Mock<ILogger<DeleteCategoryHandler>>();

            _unitOfWork.Setup(u => u.Categories).Returns(_categories.Object);
            _unitOfWork.Setup(u => u.Products).Returns(_products.Object);

            _getAllHandler = new GetAllCategoriesHandler(_unitOfWork.Object, _getAllLogger.Object);
            _createHandler = new CreateCategoryHandler(_unitOfWork.Object, _createLogger.Object);
            _updateHandler = new UpdateCategoryHandler(_unitOfWork.Object, _updateLogger.Object);
            _deleteHandler = new DeleteCategoryHandler(_unitOfWork.Object, _deleteLogger.Object);
        }

        [Test]
        public async Task EnsureGetAllCategoriesHandlerReturnsDtosWhenCategoriesExist()
        {
            var chairsId = Guid.NewGuid();
            var tablesId = Guid.NewGuid();
            _categories.Setup(r => r.GetAllAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                [
                    new Category { CategoryId = chairsId, CategoryName = "Chairs" },
                    new Category { CategoryId = tablesId, CategoryName = "Tables" }
                ]);

            var result = await _getAllHandler.Handle(It.IsAny<GetAllCategoriesQuery>(), It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.Count, Is.EqualTo(2));
                Assert.That(result[0].CategoryId, Is.EqualTo(chairsId));
                Assert.That(result[0].CategoryName, Is.EqualTo("Chairs"));
                Assert.That(result[1].CategoryId, Is.EqualTo(tablesId));
                Assert.That(result[1].CategoryName, Is.EqualTo("Tables"));
            });
        }

        [Test]
        public async Task EnsureGetAllCategoriesHandlerReturnsEmptyListWhenNoCategoriesExist()
        {
            _categories.Setup(r => r.GetAllAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            var result = await _getAllHandler.Handle(It.IsAny<GetAllCategoriesQuery>(), It.IsAny<CancellationToken>());

            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task EnsureCreateCategoryHandlerSavesCategoryWhenCommandIsValid()
        {
            _categories.Setup(r => r.AddAsync(
                    It.IsAny<Category>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _unitOfWork.Setup(u => u.CompleteAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var result = await _createHandler.Handle(
                new CreateCategoryCommand { CategoryName = "Sofas" },
                It.IsAny<CancellationToken>());

            Assert.That(result, Is.TypeOf<Guid>());
            _categories.Verify(r => r.AddAsync(It.Is<Category>(c => c.CategoryName == "Sofas"), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task EnsureCreateCategoryHandlerThrowsConflictWhenNameAlreadyExists()
        {
            _categories.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Category, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Category { CategoryName = "Sofas" });

            var exception = await Assert.ThrowsAsync<ConflictException>(() => _createHandler.Handle(
                new CreateCategoryCommand { CategoryName = "Sofas" },
                It.IsAny<CancellationToken>()));

            Assert.That(exception!.Message, Does.Contain("already exists"));
            _categories.Verify(r => r.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task EnsureUpdateCategoryHandlerUpdatesNameWhenCategoryExists()
        {
            var category = new Category { CategoryName = "Chairs" };
            _categories.SetupSequence(r => r.FindAsync(
                    It.IsAny<Expression<Func<Category, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(category)
                .ReturnsAsync((Category?)null);
            _unitOfWork.Setup(u => u.CompleteAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var result = await _updateHandler.Handle(
                new UpdateCategoryCommand { CategoryId = Guid.NewGuid(), CategoryName = "Sofas" },
                It.IsAny<CancellationToken>());

            Assert.That(result, Is.True);
            Assert.That(category.CategoryName, Is.EqualTo("Sofas"));
            Assert.That(category.UpdatedAt, Is.Not.Null);
            _categories.Verify(r => r.Update(category), Times.Once);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task EnsureUpdateCategoryHandlerReturnsFalseWhenCategoryDoesNotExist()
        {
            _categories.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Category, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Category?)null);

            var result = await _updateHandler.Handle(
                new UpdateCategoryCommand { CategoryId = Guid.NewGuid(), CategoryName = "Sofas" },
                It.IsAny<CancellationToken>());

            Assert.That(result, Is.False);
            _categories.Verify(r => r.Update(It.IsAny<Category>()), Times.Never);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task EnsureUpdateCategoryHandlerThrowsConflictWhenNameAlreadyExists()
        {
            var category = new Category { CategoryName = "Chairs" };
            _categories.SetupSequence(r => r.FindAsync(
                    It.IsAny<Expression<Func<Category, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(category)
                .ReturnsAsync(new Category { CategoryName = "Sofas" });

            var exception = await Assert.ThrowsAsync<ConflictException>(() => _updateHandler.Handle(
                new UpdateCategoryCommand { CategoryId = category.CategoryId, CategoryName = "Sofas" },
                It.IsAny<CancellationToken>()));

            Assert.That(exception!.Message, Does.Contain("already exists"));
            _categories.Verify(r => r.Update(It.IsAny<Category>()), Times.Never);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task EnsureDeleteCategoryHandlerDeletesCategoryWhenItExists()
        {
            var category = new Category { CategoryName = "Chairs" };
            _categories.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Category, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(category);
            _products.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Product, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Product?)null);
            _unitOfWork.Setup(u => u.CompleteAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var result = await _deleteHandler.Handle(
                new DeleteCategoryCommand(Guid.NewGuid()),
                It.IsAny<CancellationToken>());

            Assert.That(result, Is.True);
            _categories.Verify(r => r.Delete(category), Times.Once);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task EnsureDeleteCategoryHandlerThrowsConflictWhenCategoryHasProducts()
        {
            var category = new Category { CategoryName = "Chairs" };
            _categories.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Category, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(category);
            _products.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Product, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Product { CategoryId = category.CategoryId });

            var exception = await Assert.ThrowsAsync<ConflictException>(() => _deleteHandler.Handle(
                new DeleteCategoryCommand(category.CategoryId),
                It.IsAny<CancellationToken>()));

            Assert.That(exception!.Message, Does.Contain("products"));
            _categories.Verify(r => r.Delete(It.IsAny<Category>()), Times.Never);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task EnsureDeleteCategoryHandlerReturnsFalseWhenCategoryDoesNotExist()
        {
            _categories.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Category, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Category?)null);

            var result = await _deleteHandler.Handle(
                new DeleteCategoryCommand(Guid.NewGuid()),
                It.IsAny<CancellationToken>());

            Assert.That(result, Is.False);
            _categories.Verify(r => r.Delete(It.IsAny<Category>()), Times.Never);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
