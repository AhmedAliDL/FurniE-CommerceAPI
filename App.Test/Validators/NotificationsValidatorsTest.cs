using App.Application.Notifications.Command.MakeNotificationRead;
using App.Application.Notifications.Queries.GetUserNotifications;
using NUnit.Framework;

namespace App.Test.Validators
{
    [TestFixture]
    [Category("Notifications")]
    public class NotificationsValidatorsTest
    {
        private MakeNotificationReadValidator _validator = null!;

        [SetUp]
        public void Setup()
        {
            _validator = new MakeNotificationReadValidator();
        }

        [Test]
        public void EnsureThatMakeNotificationReadCommandIsValidWhenNotificationIdProvided()
        {
            var command = new MakeNotificationReadCommand(Guid.NewGuid());

            var result = _validator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatMakeNotificationReadCommandIsInvalidWhenNotificationIdMissing()
        {
            var command = new MakeNotificationReadCommand(Guid.Empty);

            var result = _validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatGetUserNotificationsQueryIsValidWithDefaultValues()
        {
            var validator = new GetUserNotificationsValidator();
            var query = new GetUserNotificationsQuery();

            var result = validator.Validate(query);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatGetUserNotificationsQueryIsInvalidWhenPageIsZero()
        {
            var validator = new GetUserNotificationsValidator();
            var query = new GetUserNotificationsQuery(Page: 0);

            var result = validator.Validate(query);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatGetUserNotificationsQueryIsInvalidWhenPageSizeExceedsMaximum()
        {
            var validator = new GetUserNotificationsValidator();
            var query = new GetUserNotificationsQuery(PageSize: 101);

            var result = validator.Validate(query);

            Assert.That(result.IsValid, Is.False);
        }
    }
}
