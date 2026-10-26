using App.Application.Categories.Command.CreateCategory;
using App.Application.Categories.Command.DeleteCategory;
using App.Application.Categories.Command.UpdateCategory;
using App.Application.Categories.Dto;
using App.Application.Categories.Queries.GetAllCategories;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using App.Api.Controllers;

namespace App.Test.Controllers
{
    [TestFixture]
    [Category("Categories")]
    public class CategoryControllerTest
    {
        private Mock<IMediator> _sender = null!;
        private CategoryController _controller = null!;

        [SetUp]
        public void Setup()
        {
            _sender = new Mock<IMediator>();
            _controller = new CategoryController(_sender.Object);
        }

        [Test]
        public async Task EnsureThatGetAllReturnsOkWhenCategoriesExist()
        {
            var categories = new List<CategoryDto>
            {
                new() { CategoryId = Guid.NewGuid(), CategoryName = "Chairs" },
                new() { CategoryId = Guid.NewGuid(), CategoryName = "Tables" }
            };
            _sender.Setup(s => s.Send(
                    It.IsAny<GetAllCategoriesQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(categories);

            var result = await _controller.GetAll();

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatCreateReturnsCreatedWhenCategoryIsValid()
        {
            var command = new CreateCategoryCommand { CategoryName = "Chairs" };
            _sender.Setup(s => s.Send(
                    It.IsAny<CreateCategoryCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Guid.NewGuid());

            var result = await _controller.Create(command);

            Assert.That(result, Is.TypeOf<CreatedAtActionResult>());
        }

        [Test]
        public async Task EnsureThatUpdateReturnsNoContentWhenCategoryIsUpdated()
        {
            var command = new UpdateCategoryCommand { CategoryId = Guid.NewGuid(), CategoryName = "Sofas" };
            _sender.Setup(s => s.Send(
                    It.IsAny<UpdateCategoryCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var result = await _controller.Update(command.CategoryId, command);

            Assert.That(result, Is.TypeOf<NoContentResult>());
        }

        [Test]
        public async Task EnsureThatUpdateReturnsNotFoundWhenCategoryDoesNotExist()
        {
            var command = new UpdateCategoryCommand { CategoryId = Guid.NewGuid(), CategoryName = "Sofas" };
            _sender.Setup(s => s.Send(
                    It.IsAny<UpdateCategoryCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var result = await _controller.Update(command.CategoryId, command);

            Assert.That(result, Is.TypeOf<NotFoundResult>());
        }

        [Test]
        public async Task EnsureThatUpdateReturnsBadRequestWhenRouteAndBodyIdsDiffer()
        {
            var command = new UpdateCategoryCommand { CategoryId = Guid.NewGuid(), CategoryName = "Sofas" };

            var result = await _controller.Update(Guid.NewGuid(), command);

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
            _sender.Verify(s => s.Send(It.IsAny<UpdateCategoryCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task EnsureThatDeleteReturnsNoContentWhenCategoryIsDeleted()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<DeleteCategoryCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var result = await _controller.Delete(Guid.NewGuid());

            Assert.That(result, Is.TypeOf<NoContentResult>());
        }

        [Test]
        public async Task EnsureThatDeleteReturnsNotFoundWhenCategoryDoesNotExist()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<DeleteCategoryCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var result = await _controller.Delete(Guid.NewGuid());

            Assert.That(result, Is.TypeOf<NotFoundResult>());
        }
    }
}
