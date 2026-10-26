using App.Application.AuditLogs.Queries.GetAuditLogs;
using NUnit.Framework;

namespace App.Test.Validators
{
    [TestFixture]
    [Category("AuditLogs")]
    public class AuditLogsValidatorsTest
    {
        [Test]
        public void EnsureThatGetAuditLogsQueryIsValidWithDefaults()
        {
            var validator = new GetAuditLogsValidator();

            var result = validator.Validate(new GetAuditLogsQuery());

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EnsureThatGetAuditLogsQueryIsInvalidWhenPageIsZero()
        {
            var validator = new GetAuditLogsValidator();

            var result = validator.Validate(new GetAuditLogsQuery { Page = 0 });

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatGetAuditLogsQueryIsInvalidWhenPageSizeOutOfRange()
        {
            var validator = new GetAuditLogsValidator();

            var result = validator.Validate(new GetAuditLogsQuery { PageSize = 0 });

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void EnsureThatGetAuditLogsQueryIsInvalidWhenEntityNameTooLong()
        {
            var validator = new GetAuditLogsValidator();

            var result = validator.Validate(new GetAuditLogsQuery { EntityName = new string('a', 201) });

            Assert.That(result.IsValid, Is.False);
        }
    }
}
