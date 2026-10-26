using App.Api.Controllers;
using App.Application.Responses;
using App.Application.Users.Command.UpdateUserStatus;
using App.Application.Users.Dto;
using App.Application.Users.Queries.GetUsers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace App.Test.Controllers
{
    [TestFixture]
    [Category("Users")]
    public class UserControllerTest
    {
        private Mock<IMediator> _sender = null!;
        private UserController _controller = null!;

        [SetUp]
        public void Setup()
        {
            _sender = new Mock<IMediator>();
            _controller = new UserController(_sender.Object);
        }

        [Test]
        public async Task EnsureThatGetUsersReturnsOkWhenUsersExist()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<GetUsersQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedResult<UserDto> { Items = [], TotalCount = 0 });

            var result = await _controller.GetUsers(new GetUsersQuery());

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatUpdateStatusReturnsNoContentWhenUserUpdated()
        {
            var userId = Guid.NewGuid();
            var command = new UpdateUserStatusCommand { UserId = userId, IsActive = false };
            _sender.Setup(s => s.Send(
                    It.IsAny<UpdateUserStatusCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var result = await _controller.UpdateStatus(userId, command);

            Assert.That(result, Is.TypeOf<NoContentResult>());
        }

        [Test]
        public async Task EnsureThatUpdateStatusReturnsNotFoundWhenUserDoesNotExist()
        {
            var userId = Guid.NewGuid();
            var command = new UpdateUserStatusCommand { UserId = userId, IsActive = false };
            _sender.Setup(s => s.Send(
                    It.IsAny<UpdateUserStatusCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var result = await _controller.UpdateStatus(userId, command);

            Assert.That(result, Is.TypeOf<NotFoundResult>());
        }

        [Test]
        public async Task EnsureThatUpdateStatusReturnsBadRequestWhenRouteIdMismatch()
        {
            var command = new UpdateUserStatusCommand { UserId = Guid.NewGuid(), IsActive = false };

            var result = await _controller.UpdateStatus(Guid.NewGuid(), command);

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
            _sender.Verify(s => s.Send(It.IsAny<UpdateUserStatusCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
