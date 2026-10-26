using App.Application.AuditLogs.Queries.GetAuditLogs;
using App.Application.Common.Enums;
using App.Application.Common.Interfaces.Repos;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Linq.Expressions;

namespace App.Test.Handlers
{
    [TestFixture]
    [Category("AuditLogs")]
    public class AuditLogsHandlerTest
    {
        private Mock<IUnitOfWork> _unitOfWork = null!;
        private Mock<IBaseRepository<AuditLog>> _auditLogs = null!;
        private GetAuditLogsHandler _handler = null!;

        [SetUp]
        public void Setup()
        {
            _unitOfWork = new Mock<IUnitOfWork>();
            _auditLogs = new Mock<IBaseRepository<AuditLog>>();
            _unitOfWork.Setup(u => u.AuditLogs).Returns(_auditLogs.Object);

            _handler = new GetAuditLogsHandler(_unitOfWork.Object, new Mock<ILogger<GetAuditLogsHandler>>().Object);
        }

        [Test]
        public async Task EnsureGetAuditLogsHandlerReturnsPagedDtosNewestFirst()
        {
            var user = new AppUser { Id = Guid.NewGuid(), Email = "admin@furni.test" };
            _auditLogs.Setup(r => r.FindPagedAsync(
                    It.IsAny<Expression<Func<AuditLog, bool>>>(),
                    It.IsAny<Expression<Func<AuditLog, object>>[]>(),
                    SortDirection.Descending,
                    It.IsAny<Expression<Func<AuditLog, object>>>(),
                    1,
                    20,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(((IReadOnlyList<AuditLog>)[
                    new AuditLog
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        User = user,
                        Action = "Deleted",
                        EntityName = "Notification",
                        EntityId = Guid.NewGuid(),
                        OldValues = "{\"Status\":\"Unread\"}",
                        NewValues = null
                    }
                ], 1));

            var result = await _handler.Handle(new GetAuditLogsQuery(), It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.TotalCount, Is.EqualTo(1));
                Assert.That(result.Items.Count, Is.EqualTo(1));
                Assert.That(result.Items[0].UserEmail, Is.EqualTo("admin@furni.test"));
                Assert.That(result.Items[0].Action, Is.EqualTo("Deleted"));
                Assert.That(result.Items[0].EntityName, Is.EqualTo("Notification"));
                Assert.That(result.Items[0].OldValues, Does.Contain("Unread"));
            });
        }

        [Test]
        public async Task EnsureGetAuditLogsHandlerReturnsEmptyPageWhenNoEntriesExist()
        {
            _auditLogs.Setup(r => r.FindPagedAsync(
                    It.IsAny<Expression<Func<AuditLog, bool>>>(),
                    It.IsAny<Expression<Func<AuditLog, object>>[]>(),
                    It.IsAny<SortDirection>(),
                    It.IsAny<Expression<Func<AuditLog, object>>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(((IReadOnlyList<AuditLog>)[], 0));

            var result = await _handler.Handle(new GetAuditLogsQuery { EntityName = "Notification" }, It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.Items, Is.Empty);
                Assert.That(result.TotalCount, Is.Zero);
            });
        }
    }
}
