using App.Application.Roles.Command.AssignRoleToUser;
using App.Application.Roles.Command.CreateRole;
using App.Application.Roles.Command.DeleteRole;
using App.Application.Roles.Command.UnassignRoleFromUser;
using App.Application.Roles.Dto;
using App.Application.Roles.Queries.GetRoles;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using App.Api.Controllers;

namespace App.Test.Controllers
{
    [TestFixture]
    [Category("Roles")]
    public class RoleControllerTest
    {
        private Mock<IMediator> _sender = null!;
        private RoleController _controller = null!;

        [SetUp]
        public void Setup()
        {
            _sender = new Mock<IMediator>();
            _controller = new RoleController(_sender.Object);
        }

        [Test]
        public async Task EnsureThatGetRolesReturnsOkWhenRolesExist()
        {
            var roles = new List<RoleDto>();
            _sender.Setup(s => s.Send(
                    It.IsAny<GetRolesQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(roles);

            var result = await _controller.GetRoles();

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatCreateRoleReturnsCreatedWhenRoleIsValid()
        {
            var command = new CreateRoleCommand { RoleName = "Moderator" };
            _sender.Setup(s => s.Send(
                    It.IsAny<CreateRoleCommand>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.CreateRole(command);

            Assert.That(result, Is.TypeOf<StatusCodeResult>());
            Assert.That(((StatusCodeResult)result).StatusCode, Is.EqualTo(Microsoft.AspNetCore.Http.StatusCodes.Status201Created));
        }

        [Test]
        public async Task EnsureThatAssignRoleReturnsNoContentWhenRoleAssigned()
        {
            var command = new AssignRoleToUserCommand { RoleName = "Moderator", UserEmail = "ahmed@gmail.com" };
            _sender.Setup(s => s.Send(
                    It.IsAny<AssignRoleToUserCommand>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.AssignRole(command);

            Assert.That(result, Is.TypeOf<NoContentResult>());
        }

        [Test]
        public async Task EnsureThatUnassignRoleReturnsNoContentWhenRoleRemoved()
        {
            var command = new UnassignRoleFromUserCommand { RoleName = "Moderator", UserEmail = "ahmed@gmail.com" };
            _sender.Setup(s => s.Send(
                    It.IsAny<UnassignRoleFromUserCommand>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.UnassignRole(command);

            Assert.That(result, Is.TypeOf<NoContentResult>());
        }

        [Test]
        public async Task EnsureThatDeleteRoleReturnsNoContentWhenRoleDeleted()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<DeleteRoleCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var result = await _controller.DeleteRole("Moderator");

            Assert.That(result, Is.TypeOf<NoContentResult>());
        }

        [Test]
        public async Task EnsureThatDeleteRoleReturnsNotFoundWhenRoleDoesNotExist()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<DeleteRoleCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var result = await _controller.DeleteRole("Ghost");

            Assert.That(result, Is.TypeOf<NotFoundResult>());
        }
    }
}
