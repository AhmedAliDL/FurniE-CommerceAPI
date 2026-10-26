using App.Api.Controllers;
using App.Application.AuditLogs.Queries.GetAuditLogs;
using App.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace App.Test.Controllers
{
    [TestFixture]
    [Category("AuditLogs")]
    public class AuditLogsControllerTest
    {
        private Mock<IMediator> _sender = null!;
        private AuditLogsController _controller = null!;

        [SetUp]
        public void Setup()
        {
            _sender = new Mock<IMediator>();
            _controller = new AuditLogsController(_sender.Object);
        }

        [Test]
        public async Task EnsureThatGetAuditLogsReturnsOkWhenEntriesExist()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<GetAuditLogsQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedResult<App.Application.AuditLogs.Dto.AuditLogDto> { Items = [], TotalCount = 0 });

            var result = await _controller.GetAuditLogs(new GetAuditLogsQuery());

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }
    }
}
