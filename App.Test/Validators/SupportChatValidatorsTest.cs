using App.Application.SupportChat.Command.CloseConversation;
using App.Application.SupportChat.Command.MarkConversationRead;
using App.Application.SupportChat.Command.ReopenConversation;
using App.Application.SupportChat.Command.SendMessage;
using App.Application.SupportChat.Queries.GetConversationMessages;
using NUnit.Framework;

namespace App.Test.Validators
{
    [TestFixture]
    [Category("SupportChat")]
    public class SupportChatValidatorsTest
    {
        [Test]
        public void EnsureThatMarkConversationReadCommandIsValidWhenTicketIdProvided()
        {
            var validator = new MarkConversationReadValidator();
            var command = new MarkConversationReadCommand(Guid.NewGuid());

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatMarkConversationReadCommandIsInvalidWhenTicketIdMissing()
        {
            var validator = new MarkConversationReadValidator();
            var command = new MarkConversationReadCommand(Guid.Empty);

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatCloseConversationCommandIsValidWhenTicketIdProvided()
        {
            var validator = new CloseConversationValidator();
            var command = new CloseConversationCommand(Guid.NewGuid());

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatCloseConversationCommandIsInvalidWhenTicketIdMissing()
        {
            var validator = new CloseConversationValidator();
            var command = new CloseConversationCommand(Guid.Empty);

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatReopenConversationCommandIsValidWhenTicketIdProvided()
        {
            var validator = new ReopenConversationValidator();
            var command = new ReopenConversationCommand(Guid.NewGuid());

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatReopenConversationCommandIsInvalidWhenTicketIdMissing()
        {
            var validator = new ReopenConversationValidator();
            var command = new ReopenConversationCommand(Guid.Empty);

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatSendMessageCommandIsValidWhenAllFieldsProvided()
        {
            var validator = new SendMessageValidator();
            var command = new SendMessageCommand(Guid.NewGuid(), Guid.NewGuid(), "Hello, I need help");

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatSendMessageCommandIsInvalidWhenContentMissing()
        {
            var validator = new SendMessageValidator();
            var command = new SendMessageCommand(Guid.NewGuid(), Guid.NewGuid(), "");

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatSendMessageCommandIsInvalidWhenContentTooLong()
        {
            var validator = new SendMessageValidator();
            var command = new SendMessageCommand(Guid.NewGuid(), Guid.NewGuid(), new string('a', 2001));

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatSendMessageCommandIsInvalidWhenSenderMissing()
        {
            var validator = new SendMessageValidator();
            var command = new SendMessageCommand(Guid.NewGuid(), Guid.Empty, "Hello, I need help");

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatGetConversationMessagesQueryIsValidWithDefaultValues()
        {
            var validator = new GetConversationMessagesValidator();
            var query = new GetConversationMessagesQuery(Guid.NewGuid());

            var result = validator.Validate(query);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatGetConversationMessagesQueryIsInvalidWhenTicketIdMissing()
        {
            var validator = new GetConversationMessagesValidator();
            var query = new GetConversationMessagesQuery(Guid.Empty);

            var result = validator.Validate(query);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatGetConversationMessagesQueryIsInvalidWhenPageSizeExceedsMaximum()
        {
            var validator = new GetConversationMessagesValidator();
            var query = new GetConversationMessagesQuery(Guid.NewGuid(), PageSize: 101);

            var result = validator.Validate(query);

            Assert.That(result.IsValid, Is.False);
        }
    }
}
