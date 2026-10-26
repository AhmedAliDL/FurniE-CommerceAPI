using App.Application.Contact.Command.SendContactEmail;
using NUnit.Framework;

namespace App.Test.Validators
{
    [TestFixture]
    [Category("Contact")]
    public class ContactValidatorsTest
    {
        private SendContactEmailValidator _validator = null!;

        [SetUp]
        public void Setup()
        {
            _validator = new SendContactEmailValidator();
        }

        [Test]
        public void EnsureThatSendContactEmailCommandIsValidWhenFieldsProvided()
        {
            var command = new SendContactEmailCommand { Subject = "Order inquiry", Body = "Where is my order?" };

            var result = _validator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatSendContactEmailCommandIsInvalidWhenSubjectTooShort()
        {
            var command = new SendContactEmailCommand { Subject = "Hi", Body = "Where is my order?" };

            var result = _validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatSendContactEmailCommandIsInvalidWhenSubjectTooLong()
        {
            var command = new SendContactEmailCommand { Subject = new string('a', 41), Body = "Where is my order?" };

            var result = _validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatSendContactEmailCommandIsInvalidWhenBodyTooShort()
        {
            var command = new SendContactEmailCommand { Subject = "Order inquiry", Body = "Hi" };

            var result = _validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatSendContactEmailCommandIsInvalidWhenBodyMissing()
        {
            var command = new SendContactEmailCommand { Subject = "Order inquiry", Body = "" };

            var result = _validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }
    }
}
