using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Repos;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Notifications.Command.MakeNotificationRead;
using App.Application.Notifications.Queries.GetUserNotifications;
using App.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Linq.Expressions;

namespace App.Test.Handlers
{
    [TestFixture]
    [Category("Notifications")]
    public class NotificationsHandlerTest
    {
        private Mock<IUnitOfWork> _unitOfWork = null!;
        private Mock<IBaseRepository<Notification>> _notifications = null!;
        private Mock<ICurrentUserService> _currentUserService = null!;
        private GetUserNotificationsHandler _getUserNotificationsHandler = null!;
        private MakeNotificationReadHandler _makeNotificationReadHandler = null!;

        [SetUp]
        public void Setup()
        {
            _unitOfWork = new Mock<IUnitOfWork>();
            _notifications = new Mock<IBaseRepository<Notification>>();
            _currentUserService = new Mock<ICurrentUserService>();
            _unitOfWork.Setup(u => u.Notifications).Returns(_notifications.Object);

            _getUserNotificationsHandler = new GetUserNotificationsHandler(_unitOfWork.Object, _currentUserService.Object, new Mock<ILogger<GetUserNotificationsHandler>>().Object);
            _makeNotificationReadHandler = new MakeNotificationReadHandler(_unitOfWork.Object, _currentUserService.Object, new Mock<ILogger<MakeNotificationReadHandler>>().Object);
        }

        [Test]
        public void EnsureGetUserNotificationsHandlerThrowsNotFoundWhenUserNotResolved()
        {
            _currentUserService.Setup(s => s.UserId).Returns((Guid?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _getUserNotificationsHandler.Handle(It.IsAny<GetUserNotificationsQuery>(), It.IsAny<CancellationToken>()));
        }

        [Test]
        public async Task EnsureGetUserNotificationsHandlerReturnsDtosWhenNotificationsExist()
        {
            var userId = Guid.NewGuid();
            _currentUserService.Setup(s => s.UserId).Returns(userId);
            _notifications.Setup(r => r.FindPagedAsync(
                    It.IsAny<Expression<Func<Notification, bool>>>(),
                    It.IsAny<Expression<Func<Notification, object>>[]>(),
                    It.IsAny<App.Application.Common.Enums.SortDirection>(),
                    It.IsAny<Expression<Func<Notification, object>>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(((IReadOnlyList<Notification>)
                [
                    new Notification { Id = Guid.NewGuid(), ReceiverId = userId, SenderId = Guid.NewGuid(), Message = "Your order has shipped.", Link = "/orders", IsRead = false },
                    new Notification { Id = Guid.NewGuid(), ReceiverId = userId, SenderId = Guid.NewGuid(), Message = "Your order has been delivered.", IsRead = true }
                ], 2));

            var result = await _getUserNotificationsHandler.Handle(new GetUserNotificationsQuery(), It.IsAny<CancellationToken>());

            Assert.That(result.TotalCount, Is.EqualTo(2));
            Assert.That(result.Items, Has.Some.Property("Message").EqualTo("Your order has shipped."));
            Assert.That(result.Items, Has.Some.Property("IsRead").EqualTo(true));
        }

        [Test]
        public async Task EnsureGetUserNotificationsHandlerReturnsEmptyListWhenNoNotificationsExist()
        {
            _currentUserService.Setup(s => s.UserId).Returns(Guid.NewGuid());
            _notifications.Setup(r => r.FindPagedAsync(
                    It.IsAny<Expression<Func<Notification, bool>>>(),
                    It.IsAny<Expression<Func<Notification, object>>[]>(),
                    It.IsAny<App.Application.Common.Enums.SortDirection>(),
                    It.IsAny<Expression<Func<Notification, object>>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(((IReadOnlyList<Notification>)[], 0));

            var result = await _getUserNotificationsHandler.Handle(new GetUserNotificationsQuery(), It.IsAny<CancellationToken>());

            Assert.That(result.Items, Is.Empty);
            Assert.That(result.TotalCount, Is.EqualTo(0));
        }

        [Test]
        public async Task EnsureMakeNotificationReadHandlerMarksNotificationReadWhenItBelongsToUser()
        {
            var userId = Guid.NewGuid();
            _currentUserService.Setup(s => s.UserId).Returns(userId);
            _notifications.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Notification, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Notification { Id = Guid.NewGuid(), ReceiverId = userId, IsRead = false });
            _unitOfWork.Setup(u => u.CompleteAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var result = await _makeNotificationReadHandler.Handle(new MakeNotificationReadCommand(Guid.NewGuid()), It.IsAny<CancellationToken>());

            Assert.That(result, Is.EqualTo(1));
            _notifications.Verify(r => r.Update(It.Is<Notification>(n => n.IsRead)), Times.Once);
        }

        [Test]
        public void EnsureMakeNotificationReadHandlerThrowsNotFoundWhenNotificationNotExists()
        {
            _currentUserService.Setup(s => s.UserId).Returns(Guid.NewGuid());
            _notifications.Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Notification, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Notification?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _makeNotificationReadHandler.Handle(new MakeNotificationReadCommand(Guid.NewGuid()), It.IsAny<CancellationToken>()));
        }

        [Test]
        public void EnsureMakeNotificationReadHandlerThrowsNotFoundWhenUserNotResolved()
        {
            _currentUserService.Setup(s => s.UserId).Returns((Guid?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _makeNotificationReadHandler.Handle(new MakeNotificationReadCommand(Guid.NewGuid()), It.IsAny<CancellationToken>()));
        }
    }
}
