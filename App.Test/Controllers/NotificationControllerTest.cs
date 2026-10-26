using App.Application.Notifications.Command.MakeNotificationRead;
using App.Application.Notifications.Dto;
using App.Application.Notifications.Queries.GetUserNotifications;
using App.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using App.Api.Controllers;

namespace App.Test.Controllers
{
    [TestFixture]
    [Category("Notifications")]
    public class NotificationControllerTest
    {
        private Mock<IMediator> _sender = null!;
        private NotificationController _controller = null!;

        [SetUp]
        public void Setup()
        {
            _sender = new Mock<IMediator>();
            _controller = new NotificationController(_sender.Object);
        }

        [Test]
        public async Task EnsureThatGetAdminNotificationsReturnsOkWhenNotificationsExist()
        {
            var notifications = new PagedResult<UserNotificationDto> { Items = [new()], Page = 1, PageSize = 20, TotalCount = 1 };
            _sender.Setup(s => s.Send(
                    It.IsAny<GetUserNotificationsQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(notifications);

            var result = await _controller.GetAdminNotifications();

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatGetAdminNotificationsReturnsOkWhenNoNotifications()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<GetUserNotificationsQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedResult<UserNotificationDto> { Page = 1, PageSize = 20, TotalCount = 0 });

            var result = await _controller.GetAdminNotifications();

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatMakeNotificationReadReturnsOkWhenNotificationUpdated()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<MakeNotificationReadCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var result = await _controller.MakeNotificationRead(Guid.NewGuid());

            Assert.That(result, Is.TypeOf<OkResult>());
        }

        [Test]
        public async Task EnsureThatMakeNotificationReadReturnsBadRequestWhenUpdateFails()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<MakeNotificationReadCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            var result = await _controller.MakeNotificationRead(Guid.NewGuid());

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        }
    }
}
