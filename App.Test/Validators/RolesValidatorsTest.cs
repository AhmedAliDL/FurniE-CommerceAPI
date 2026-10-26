using App.Application.Roles.Command.AssignRoleToUser;
using App.Application.Roles.Command.CreateRole;
using App.Application.Roles.Command.DeleteRole;
using App.Application.Roles.Command.UnassignRoleFromUser;
using NUnit.Framework;

namespace App.Test.Validators
{
    [TestFixture]
    [Category("Roles")]
    public class RolesValidatorsTest
    {
        [Test]
        public void EnsureThatCreateRoleCommandIsValidWhenNameProvided()
        {
            var validator = new CreateRoleValidator();
            var command = new CreateRoleCommand { RoleName = "SupportAgent", RoleDescription = "Handles support tickets" };

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatCreateRoleCommandIsInvalidWhenNameMissing()
        {
            var validator = new CreateRoleValidator();
            var command = new CreateRoleCommand { RoleName = "" };

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatCreateRoleCommandIsInvalidWhenDescriptionTooLong()
        {
            var validator = new CreateRoleValidator();
            var command = new CreateRoleCommand { RoleName = "SupportAgent", RoleDescription = new string('a', 256) };

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatAssignRoleToUserCommandIsValidWhenFieldsProvided()
        {
            var validator = new AssignRoleToUserValidator();
            var command = new AssignRoleToUserCommand { RoleName = "Admin", UserEmail = "ahmed@gmail.com" };

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatAssignRoleToUserCommandIsInvalidWhenEmailFormatIsWrong()
        {
            var validator = new AssignRoleToUserValidator();
            var command = new AssignRoleToUserCommand { RoleName = "Admin", UserEmail = "not-an-email" };

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatAssignRoleToUserCommandIsInvalidWhenRoleNameMissing()
        {
            var validator = new AssignRoleToUserValidator();
            var command = new AssignRoleToUserCommand { RoleName = "", UserEmail = "ahmed@gmail.com" };

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatUnassignRoleFromUserCommandIsValidWhenFieldsProvided()
        {
            var validator = new UnassignRoleFromUserValidator();
            var command = new UnassignRoleFromUserCommand { RoleName = "SupportAgent", UserEmail = "ahmed@gmail.com" };

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatUnassignRoleFromUserCommandIsInvalidWhenEmailFormatIsWrong()
        {
            var validator = new UnassignRoleFromUserValidator();
            var command = new UnassignRoleFromUserCommand { RoleName = "SupportAgent", UserEmail = "not-an-email" };

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatUnassignRoleFromUserCommandIsInvalidWhenRoleNameMissing()
        {
            var validator = new UnassignRoleFromUserValidator();
            var command = new UnassignRoleFromUserCommand { RoleName = "", UserEmail = "ahmed@gmail.com" };

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatDeleteRoleCommandIsValidWhenNameProvided()
        {
            var validator = new DeleteRoleValidator();
            var command = new DeleteRoleCommand("Moderator");

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatDeleteRoleCommandIsInvalidWhenNameMissing()
        {
            var validator = new DeleteRoleValidator();
            var command = new DeleteRoleCommand("");

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }
    }
}
