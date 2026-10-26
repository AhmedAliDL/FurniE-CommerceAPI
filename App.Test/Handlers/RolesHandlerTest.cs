using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Roles.Command.AssignRoleToUser;
using App.Application.Roles.Command.CreateRole;
using App.Application.Roles.Command.DeleteRole;
using App.Application.Roles.Command.UnassignRoleFromUser;
using App.Application.Roles.Queries.GetRoles;
using App.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace App.Test.Handlers
{
    [TestFixture]
    [Category("Roles")]
    public class RolesHandlerTest
    {
        private Mock<IRoleService> _roleService = null!;
        private Mock<IIdentityService> _identityService = null!;

        private CreateRoleHandler _createRoleHandler = null!;
        private GetRolesHandler _getRolesHandler = null!;
        private AssignRoleToUserHandler _assignRoleToUserHandler = null!;
        private UnassignRoleFromUserHandler _unassignRoleFromUserHandler = null!;
        private DeleteRoleHandler _deleteRoleHandler = null!;

        [SetUp]
        public void Setup()
        {
            _roleService = new Mock<IRoleService>();
            _identityService = new Mock<IIdentityService>();
            _identityService.Setup(i => i.GetUsersInRoleAsync(It.IsAny<string>())).ReturnsAsync([]);

            _createRoleHandler = new CreateRoleHandler(_roleService.Object, new Mock<ILogger<CreateRoleHandler>>().Object);
            _getRolesHandler = new GetRolesHandler(_roleService.Object, new Mock<ILogger<GetRolesHandler>>().Object);
            _assignRoleToUserHandler = new AssignRoleToUserHandler(_identityService.Object, _roleService.Object, new Mock<ILogger<AssignRoleToUserHandler>>().Object);
            _unassignRoleFromUserHandler = new UnassignRoleFromUserHandler(_identityService.Object, _roleService.Object, new Mock<ILogger<UnassignRoleFromUserHandler>>().Object);
            _deleteRoleHandler = new DeleteRoleHandler(_roleService.Object, _identityService.Object, new Mock<ILogger<DeleteRoleHandler>>().Object);
        }

        [Test]
        public async Task EnsureCreateRoleHandlerCreatesRoleWhenItDoesNotExist()
        {
            _roleService.Setup(r => r.IsRoleExistsAsync("Sales")).ReturnsAsync(false);
            _roleService.Setup(r => r.CreateRoleAsync("Sales", "Sales team members")).ReturnsAsync(IdentityResult.Success);

            var request = new CreateRoleCommand { RoleName = "Sales", RoleDescription = "Sales team members" };

            await _createRoleHandler.Handle(request, It.IsAny<CancellationToken>());

            _roleService.Verify(r => r.CreateRoleAsync("Sales", "Sales team members"), Times.Once);
        }

        [Test]
        public void EnsureCreateRoleHandlerThrowsInvalidOperationWhenRoleAlreadyExists()
        {
            _roleService.Setup(r => r.IsRoleExistsAsync("Admin")).ReturnsAsync(true);

            var request = new CreateRoleCommand { RoleName = "Admin", RoleDescription = "Administrators" };

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _createRoleHandler.Handle(request, It.IsAny<CancellationToken>()));
            _roleService.Verify(r => r.CreateRoleAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void EnsureCreateRoleHandlerThrowsInvalidOperationWhenCreationFails()
        {
            _roleService.Setup(r => r.IsRoleExistsAsync("Sales")).ReturnsAsync(false);
            _roleService.Setup(r => r.CreateRoleAsync("Sales", It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Role store unavailable" }));

            var request = new CreateRoleCommand { RoleName = "Sales", RoleDescription = "Sales team members" };

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _createRoleHandler.Handle(request, It.IsAny<CancellationToken>()));
        }

        [Test]
        public async Task EnsureGetRolesHandlerReturnsAllRoles()
        {
            _roleService.Setup(r => r.GetAllRolesAsync())
                .ReturnsAsync(new List<IdentityRole<Guid>>
                {
                    new("Admin"),
                    new("Customer")
                });

            var result = await _getRolesHandler.Handle(new GetRolesQuery(), It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result, Has.Count.EqualTo(2));
                Assert.That(result[0].RoleName, Is.EqualTo("Admin"));
                Assert.That(result[1].RoleName, Is.EqualTo("Customer"));
            });
        }

        [Test]
        public async Task EnsureAssignRoleToUserHandlerAssignsRoleWhenUserAndRoleExist()
        {
            var user = new AppUser { Id = Guid.NewGuid(), Email = "ahmedali@gmail.com", FName = "Ahmed", LName = "Ali" };
            _identityService.Setup(i => i.GetUserByEmailAsync("ahmedali@gmail.com")).ReturnsAsync(user);
            _roleService.Setup(r => r.IsRoleExistsAsync("SupportAgent")).ReturnsAsync(true);
            _roleService.Setup(r => r.AssignRoleToUserAsync(user, "SupportAgent")).ReturnsAsync(IdentityResult.Success);

            var request = new AssignRoleToUserCommand { RoleName = "SupportAgent", UserEmail = "ahmedali@gmail.com" };

            await _assignRoleToUserHandler.Handle(request, It.IsAny<CancellationToken>());

            _roleService.Verify(r => r.AssignRoleToUserAsync(user, "SupportAgent"), Times.Once);
        }

        [Test]
        public void EnsureAssignRoleToUserHandlerThrowsNotFoundWhenUserNotFound()
        {
            _identityService.Setup(i => i.GetUserByEmailAsync("unknown@gmail.com")).ReturnsAsync((AppUser?)null);

            var request = new AssignRoleToUserCommand { RoleName = "SupportAgent", UserEmail = "unknown@gmail.com" };

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _assignRoleToUserHandler.Handle(request, It.IsAny<CancellationToken>()));
        }

        [Test]
        public void EnsureAssignRoleToUserHandlerThrowsInvalidRoleWhenRoleDoesNotExist()
        {
            var user = new AppUser { Id = Guid.NewGuid(), Email = "ahmedali@gmail.com", FName = "Ahmed", LName = "Ali" };
            _identityService.Setup(i => i.GetUserByEmailAsync("ahmedali@gmail.com")).ReturnsAsync(user);
            _roleService.Setup(r => r.IsRoleExistsAsync("Ghost")).ReturnsAsync(false);

            var request = new AssignRoleToUserCommand { RoleName = "Ghost", UserEmail = "ahmedali@gmail.com" };

            Assert.ThrowsAsync<InvalidRoleException>(async () =>
                await _assignRoleToUserHandler.Handle(request, It.IsAny<CancellationToken>()));
        }

        [Test]
        public void EnsureAssignRoleToUserHandlerThrowsInvalidOperationWhenAssignmentFails()
        {
            var user = new AppUser { Id = Guid.NewGuid(), Email = "ahmedali@gmail.com", FName = "Ahmed", LName = "Ali" };
            _identityService.Setup(i => i.GetUserByEmailAsync("ahmedali@gmail.com")).ReturnsAsync(user);
            _roleService.Setup(r => r.IsRoleExistsAsync("SupportAgent")).ReturnsAsync(true);
            _roleService.Setup(r => r.AssignRoleToUserAsync(user, "SupportAgent"))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Already assigned" }));

            var request = new AssignRoleToUserCommand { RoleName = "SupportAgent", UserEmail = "ahmedali@gmail.com" };

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _assignRoleToUserHandler.Handle(request, It.IsAny<CancellationToken>()));
        }

        [Test]
        public async Task EnsureUnassignRoleFromUserHandlerRemovesRoleWhenUserHasIt()
        {
            var user = new AppUser { Id = Guid.NewGuid(), Email = "ahmedali@gmail.com", FName = "Ahmed", LName = "Ali" };
            _identityService.Setup(i => i.GetUserByEmailAsync("ahmedali@gmail.com")).ReturnsAsync(user);
            _roleService.Setup(r => r.IsRoleExistsAsync("SupportAgent")).ReturnsAsync(true);
            _roleService.Setup(r => r.GetAllRolesOfUser(user)).ReturnsAsync(new List<string> { "Customer", "SupportAgent" });
            _roleService.Setup(r => r.DeleteAssignRoleAsync(user, "SupportAgent")).ReturnsAsync(true);

            var request = new UnassignRoleFromUserCommand { RoleName = "SupportAgent", UserEmail = "ahmedali@gmail.com" };

            await _unassignRoleFromUserHandler.Handle(request, It.IsAny<CancellationToken>());

            _roleService.Verify(r => r.DeleteAssignRoleAsync(user, "SupportAgent"), Times.Once);
        }

        [Test]
        public void EnsureUnassignRoleFromUserHandlerThrowsNotFoundWhenUserNotFound()
        {
            _identityService.Setup(i => i.GetUserByEmailAsync("unknown@gmail.com")).ReturnsAsync((AppUser?)null);

            var request = new UnassignRoleFromUserCommand { RoleName = "SupportAgent", UserEmail = "unknown@gmail.com" };

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _unassignRoleFromUserHandler.Handle(request, It.IsAny<CancellationToken>()));
        }

        [Test]
        public void EnsureUnassignRoleFromUserHandlerThrowsInvalidRoleWhenRoleDoesNotExist()
        {
            var user = new AppUser { Id = Guid.NewGuid(), Email = "ahmedali@gmail.com", FName = "Ahmed", LName = "Ali" };
            _identityService.Setup(i => i.GetUserByEmailAsync("ahmedali@gmail.com")).ReturnsAsync(user);
            _roleService.Setup(r => r.IsRoleExistsAsync("Ghost")).ReturnsAsync(false);

            var request = new UnassignRoleFromUserCommand { RoleName = "Ghost", UserEmail = "ahmedali@gmail.com" };

            Assert.ThrowsAsync<InvalidRoleException>(async () =>
                await _unassignRoleFromUserHandler.Handle(request, It.IsAny<CancellationToken>()));
        }

        [Test]
        public async Task EnsureUnassignRoleFromUserHandlerSkipsWhenUserDoesNotHaveRole()
        {
            var user = new AppUser { Id = Guid.NewGuid(), Email = "ahmedali@gmail.com", FName = "Ahmed", LName = "Ali" };
            _identityService.Setup(i => i.GetUserByEmailAsync("ahmedali@gmail.com")).ReturnsAsync(user);
            _roleService.Setup(r => r.IsRoleExistsAsync("SupportAgent")).ReturnsAsync(true);
            _roleService.Setup(r => r.GetAllRolesOfUser(user)).ReturnsAsync(new List<string> { "Customer" });

            var request = new UnassignRoleFromUserCommand { RoleName = "SupportAgent", UserEmail = "ahmedali@gmail.com" };

            await _unassignRoleFromUserHandler.Handle(request, It.IsAny<CancellationToken>());

            _roleService.Verify(r => r.DeleteAssignRoleAsync(It.IsAny<AppUser>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void EnsureUnassignRoleFromUserHandlerThrowsInvalidOperationWhenUnassignFails()
        {
            var user = new AppUser { Id = Guid.NewGuid(), Email = "ahmedali@gmail.com", FName = "Ahmed", LName = "Ali" };
            _identityService.Setup(i => i.GetUserByEmailAsync("ahmedali@gmail.com")).ReturnsAsync(user);
            _roleService.Setup(r => r.IsRoleExistsAsync("SupportAgent")).ReturnsAsync(true);
            _roleService.Setup(r => r.GetAllRolesOfUser(user)).ReturnsAsync(new List<string> { "SupportAgent" });
            _roleService.Setup(r => r.DeleteAssignRoleAsync(user, "SupportAgent")).ReturnsAsync(false);

            var request = new UnassignRoleFromUserCommand { RoleName = "SupportAgent", UserEmail = "ahmedali@gmail.com" };

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _unassignRoleFromUserHandler.Handle(request, It.IsAny<CancellationToken>()));
        }

        [Test]
        public async Task EnsureDeleteRoleHandlerDeletesRoleWhenNotBuiltInAndUnassigned()
        {
            var role = new IdentityRole<Guid>("Sales");
            _roleService.Setup(r => r.GetRoleAsync("Sales")).ReturnsAsync(role);
            _roleService.Setup(r => r.DeleteRoleAsync(role)).ReturnsAsync(true);

            var result = await _deleteRoleHandler.Handle(new DeleteRoleCommand("Sales"), It.IsAny<CancellationToken>());

            Assert.That(result, Is.True);
            _roleService.Verify(r => r.DeleteRoleAsync(role), Times.Once);
        }

        [Test]
        public async Task EnsureDeleteRoleHandlerReturnsFalseWhenRoleDoesNotExist()
        {
            _roleService.Setup(r => r.GetRoleAsync("Ghost")).ReturnsAsync((IdentityRole<Guid>?)null);

            var result = await _deleteRoleHandler.Handle(new DeleteRoleCommand("Ghost"), It.IsAny<CancellationToken>());

            Assert.That(result, Is.False);
            _roleService.Verify(r => r.DeleteRoleAsync(It.IsAny<IdentityRole<Guid>>()), Times.Never);
        }

        [Test]
        public void EnsureDeleteRoleHandlerThrowsConflictWhenRoleIsBuiltIn()
        {
            _roleService.Setup(r => r.GetRoleAsync("Admin")).ReturnsAsync(new IdentityRole<Guid>("Admin"));

            Assert.ThrowsAsync<ConflictException>(async () =>
                await _deleteRoleHandler.Handle(new DeleteRoleCommand("Admin"), It.IsAny<CancellationToken>()));
            _roleService.Verify(r => r.DeleteRoleAsync(It.IsAny<IdentityRole<Guid>>()), Times.Never);
        }

        [Test]
        public void EnsureDeleteRoleHandlerThrowsConflictWhenRoleHasAssignedUsers()
        {
            var role = new IdentityRole<Guid>("Sales");
            _roleService.Setup(r => r.GetRoleAsync("Sales")).ReturnsAsync(role);
            _identityService.Setup(i => i.GetUsersInRoleAsync("Sales"))
                .ReturnsAsync([new AppUser { Email = "sales@furni.test" }]);

            Assert.ThrowsAsync<ConflictException>(async () =>
                await _deleteRoleHandler.Handle(new DeleteRoleCommand("Sales"), It.IsAny<CancellationToken>()));
            _roleService.Verify(r => r.DeleteRoleAsync(It.IsAny<IdentityRole<Guid>>()), Times.Never);
        }

        [Test]
        public void EnsureDeleteRoleHandlerThrowsInvalidOperationWhenDeletionFails()
        {
            var role = new IdentityRole<Guid>("Sales");
            _roleService.Setup(r => r.GetRoleAsync("Sales")).ReturnsAsync(role);
            _roleService.Setup(r => r.DeleteRoleAsync(role)).ReturnsAsync(false);

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _deleteRoleHandler.Handle(new DeleteRoleCommand("Sales"), It.IsAny<CancellationToken>()));
        }
    }
}
