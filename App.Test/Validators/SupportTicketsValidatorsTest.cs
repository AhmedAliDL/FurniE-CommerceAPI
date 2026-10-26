using App.Application.SupportTickets.Command.CreateSupportTicket;
using App.Application.SupportTickets.Command.UpdateTicketStatus;
using App.Application.SupportTickets.Queries.GetAllTickets;
using App.Application.SupportTickets.Queries.GetMySupportTickets;
using App.Domain.Enums;
using NUnit.Framework;

namespace App.Test.Validators
{
    [TestFixture]
    [Category("SupportTickets")]
    public class SupportTicketsValidatorsTest
    {
        [Test]
        public void EnsureThatCreateSupportTicketCommandIsValidWhenSubjectProvided()
        {
            var validator = new CreateSupportTicketValidator();
            var command = new CreateSupportTicketCommand("Order not delivered");

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatCreateSupportTicketCommandIsInvalidWhenSubjectMissing()
        {
            var validator = new CreateSupportTicketValidator();
            var command = new CreateSupportTicketCommand("");

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatCreateSupportTicketCommandIsInvalidWhenSubjectTooLong()
        {
            var validator = new CreateSupportTicketValidator();
            var command = new CreateSupportTicketCommand(new string('a', 201));

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatCreateSupportTicketCommandIsInvalidWhenPriorityNotInEnum()
        {
            var validator = new CreateSupportTicketValidator();
            var command = new CreateSupportTicketCommand("Order not delivered", (SupportTicketPriority)99);

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatUpdateTicketStatusCommandIsValidWhenFieldsProvided()
        {
            var validator = new UpdateTicketStatusValidator();
            var command = new UpdateTicketStatusCommand(Guid.NewGuid(), SupportTicketStatus.InProgress);

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatUpdateTicketStatusCommandIsInvalidWhenTicketIdMissing()
        {
            var validator = new UpdateTicketStatusValidator();
            var command = new UpdateTicketStatusCommand(Guid.Empty, SupportTicketStatus.Closed);

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatUpdateTicketStatusCommandIsInvalidWhenStatusNotInEnum()
        {
            var validator = new UpdateTicketStatusValidator();
            var command = new UpdateTicketStatusCommand(Guid.NewGuid(), (SupportTicketStatus)99);

            var result = validator.Validate(command);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatGetMySupportTicketsQueryIsValidWithDefaultValues()
        {
            var validator = new GetMySupportTicketsValidator();
            var query = new GetMySupportTicketsQuery();

            var result = validator.Validate(query);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatGetMySupportTicketsQueryIsInvalidWhenPageIsZero()
        {
            var validator = new GetMySupportTicketsValidator();
            var query = new GetMySupportTicketsQuery(Page: 0);

            var result = validator.Validate(query);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatGetMySupportTicketsQueryIsInvalidWhenPageSizeExceedsMaximum()
        {
            var validator = new GetMySupportTicketsValidator();
            var query = new GetMySupportTicketsQuery(PageSize: 101);

            var result = validator.Validate(query);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatGetAllTicketsQueryIsValidWithDefaultValues()
        {
            var validator = new GetAllTicketsValidator();
            var query = new GetAllTicketsQuery();

            var result = validator.Validate(query);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatGetAllTicketsQueryIsInvalidWhenStatusNotInEnum()
        {
            var validator = new GetAllTicketsValidator();
            var query = new GetAllTicketsQuery((SupportTicketStatus)99);

            var result = validator.Validate(query);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatGetAllTicketsQueryIsInvalidWhenPageSizeExceedsMaximum()
        {
            var validator = new GetAllTicketsValidator();
            var query = new GetAllTicketsQuery(PageSize: 101);

            var result = validator.Validate(query);

            Assert.That(result.IsValid, Is.False);
        }
    }
}
