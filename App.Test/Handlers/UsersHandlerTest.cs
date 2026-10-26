using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Users.Command.UpdateUserStatus;
using App.Application.Users.Queries.GetUsers;
using App.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace App.Test.Handlers
{
    [TestFixture]
    [Category("Users")]
    public class UsersHandlerTest
    {
        private Mock<IIdentityService> _identityService = null!;
        private Mock<IRoleService> _roleService = null!;
        private Mock<ICurrentUserService> _currentUserService = null!;

        private GetUsersHandler _getUsersHandler = null!;
        private UpdateUserStatusHandler _updateUserStatusHandler = null!;

        [SetUp]
        public void Setup()
        {
            _identityService = new Mock<IIdentityService>();
            _roleService = new Mock<IRoleService>();
            _currentUserService = new Mock<ICurrentUserService>();

            _getUsersHandler = new GetUsersHandler(_identityService.Object, _roleService.Object, new Mock<ILogger<GetUsersHandler>>().Object);
            _updateUserStatusHandler = new UpdateUserStatusHandler(_identityService.Object, _currentUserService.Object, new Mock<ILogger<UpdateUserStatusHandler>>().Object);
        }

        [Test]
        public async Task EnsureGetUsersHandlerReturnsPagedDtosWithRoles()
        {
            var customer = new AppUser { Id = Guid.NewGuid(), Email = "customer@furni.test", FName = "Sara", LName = "Ali", PhoneNumber = "01001234567", IsActive = true };
            var admin = new AppUser { Id = Guid.NewGuid(), Email = "admin@furni.test", FName = "Ahmed", LName = "Hassan", IsActive = false };
            _identityService.Setup(i => i.GetUsersPagedAsync("furni", 1, 20))
                .ReturnsAsync((new List<AppUser> { customer, admin }, 2));
            _roleService.Setup(r => r.GetAllRolesOfUser(customer)).ReturnsAsync(new List<string> { "Customer" });
            _roleService.Setup(r => r.GetAllRolesOfUser(admin)).ReturnsAsync(new List<string> { "Admin" });

            var result = await _getUsersHandler.Handle(new GetUsersQuery { Page = 1, PageSize = 20, Search = "furni" }, It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.TotalCount, Is.EqualTo(2));
                Assert.That(result.Items.Count, Is.EqualTo(2));
                Assert.That(result.Items[0].Email, Is.EqualTo("customer@furni.test"));
                Assert.That(result.Items[0].Roles, Does.Contain("Customer"));
                Assert.That(result.Items[1].IsActive, Is.False);
                Assert.That(result.Items[1].Roles, Does.Contain("Admin"));
            });
        }

        [Test]
        public async Task EnsureGetUsersHandlerReturnsEmptyPageWhenNoUsersExist()
        {
            _identityService.Setup(i => i.GetUsersPagedAsync(null, 2, 10))
                .ReturnsAsync((new List<AppUser>(), 0));

            var result = await _getUsersHandler.Handle(new GetUsersQuery { Page = 2, PageSize = 10 }, It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.Items, Is.Empty);
                Assert.That(result.TotalCount, Is.Zero);
                Assert.That(result.Page, Is.EqualTo(2));
            });
        }

        [Test]
        public async Task EnsureUpdateUserStatusHandlerActivatesUser()
        {
            var user = new AppUser { Id = Guid.NewGuid(), Email = "customer@furni.test", IsActive = false };
            _identityService.Setup(i => i.GetUserByIdAsync(user.Id)).ReturnsAsync(user);
            _identityService.Setup(i => i.UpdateUserAsync(user)).ReturnsAsync(IdentityResult.Success);

            var result = await _updateUserStatusHandler.Handle(
                new UpdateUserStatusCommand { UserId = user.Id, IsActive = true },
                It.IsAny<CancellationToken>());

            Assert.That(result, Is.True);
            Assert.That(user.IsActive, Is.True);
            _identityService.Verify(i => i.UpdateUserAsync(user), Times.Once);
        }

        [Test]
        public async Task EnsureUpdateUserStatusHandlerDeactivatesAnotherUser()
        {
            var user = new AppUser { Id = Guid.NewGuid(), Email = "customer@furni.test", IsActive = true };
            _currentUserService.Setup(s => s.UserId).Returns(Guid.NewGuid());
            _identityService.Setup(i => i.GetUserByIdAsync(user.Id)).ReturnsAsync(user);
            _identityService.Setup(i => i.UpdateUserAsync(user)).ReturnsAsync(IdentityResult.Success);

            var result = await _updateUserStatusHandler.Handle(
                new UpdateUserStatusCommand { UserId = user.Id, IsActive = false },
                It.IsAny<CancellationToken>());

            Assert.That(result, Is.True);
            Assert.That(user.IsActive, Is.False);
        }

        [Test]
        public void EnsureUpdateUserStatusHandlerThrowsConflictWhenDeactivatingSelf()
        {
            var userId = Guid.NewGuid();
            _currentUserService.Setup(s => s.UserId).Returns(userId);
            _identityService.Setup(i => i.GetUserByIdAsync(userId))
                .ReturnsAsync(new AppUser { Id = userId, Email = "admin@furni.test", IsActive = true });

            Assert.ThrowsAsync<ConflictException>(async () =>
                await _updateUserStatusHandler.Handle(
                    new UpdateUserStatusCommand { UserId = userId, IsActive = false },
                    It.IsAny<CancellationToken>()));
            _identityService.Verify(i => i.UpdateUserAsync(It.IsAny<AppUser>()), Times.Never);
        }

        [Test]
        public async Task EnsureUpdateUserStatusHandlerReturnsFalseWhenUserNotExists()
        {
            _identityService.Setup(i => i.GetUserByIdAsync(It.IsAny<Guid>())).ReturnsAsync((AppUser?)null);

            var result = await _updateUserStatusHandler.Handle(
                new UpdateUserStatusCommand { UserId = Guid.NewGuid(), IsActive = false },
                It.IsAny<CancellationToken>());

            Assert.That(result, Is.False);
            _identityService.Verify(i => i.UpdateUserAsync(It.IsAny<AppUser>()), Times.Never);
        }

        [Test]
        public void EnsureUpdateUserStatusHandlerThrowsInvalidOperationWhenUpdateFails()
        {
            var user = new AppUser { Id = Guid.NewGuid(), Email = "customer@furni.test", IsActive = true };
            _identityService.Setup(i => i.GetUserByIdAsync(user.Id)).ReturnsAsync(user);
            _identityService.Setup(i => i.UpdateUserAsync(user))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "store unavailable" }));

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _updateUserStatusHandler.Handle(
                    new UpdateUserStatusCommand { UserId = user.Id, IsActive = false },
                    It.IsAny<CancellationToken>()));
        }
    }
}
