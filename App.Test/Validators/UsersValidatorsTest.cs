using App.Application.Users.Command.UpdateUserStatus;
using App.Application.Users.Queries.GetUsers;
using NUnit.Framework;

namespace App.Test.Validators
{
    [TestFixture]
    [Category("Users")]
    public class UsersValidatorsTest
    {
        [Test]
        public void EnsureThatGetUsersQueryIsValidWithDefaults()
        {
            var validator = new GetUsersValidator();

            var result = validator.Validate(new GetUsersQuery());

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatGetUsersQueryIsInvalidWhenPageIsZero()
        {
            var validator = new GetUsersValidator();

            var result = validator.Validate(new GetUsersQuery { Page = 0 });

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatGetUsersQueryIsInvalidWhenPageSizeOutOfRange()
        {
            var validator = new GetUsersValidator();

            var result = validator.Validate(new GetUsersQuery { PageSize = 101 });

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatGetUsersQueryIsInvalidWhenSearchTooLong()
        {
            var validator = new GetUsersValidator();

            var result = validator.Validate(new GetUsersQuery { Search = new string('a', 101) });

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatUpdateUserStatusCommandIsValidWithUserId()
        {
            var validator = new UpdateUserStatusValidator();

            var result = validator.Validate(new UpdateUserStatusCommand { UserId = Guid.NewGuid(), IsActive = false });

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatUpdateUserStatusCommandIsInvalidWhenUserIdMissing()
        {
            var validator = new UpdateUserStatusValidator();

            var result = validator.Validate(new UpdateUserStatusCommand { IsActive = true });

            Assert.That(result.IsValid, Is.False);
        }
    }
}
