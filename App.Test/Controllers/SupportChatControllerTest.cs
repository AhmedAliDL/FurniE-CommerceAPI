using App.Application.SupportChat.Command.CloseConversation;
using App.Application.SupportChat.Command.MarkConversationRead;
using App.Application.SupportChat.Command.ReopenConversation;
using App.Application.SupportChat.Dto;
using App.Application.SupportChat.Queries.GetConversation;
using App.Application.SupportChat.Queries.GetConversationMessages;
using App.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using App.Api.Controllers;

namespace App.Test.Controllers
{
    [TestFixture]
    [Category("SupportChat")]
    public class SupportChatControllerTest
    {
        private Mock<IMediator> _sender = null!;
        private SupportChatController _controller = null!;

        [SetUp]
        public void Setup()
        {
            _sender = new Mock<IMediator>();
            _controller = new SupportChatController(_sender.Object);
        }

        [Test]
        public async Task EnsureThatGetConversationReturnsOkWhenConversationExists()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<GetConversationQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SupportConversationDto());

            var result = await _controller.GetConversation(Guid.NewGuid());

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatGetMessagesReturnsOkWhenMessagesExist()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<GetConversationMessagesQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedResult<SupportMessageDto> { Page = 1, PageSize = 50, TotalCount = 0 });

            var result = await _controller.GetMessages(Guid.NewGuid());

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatMarkReadReturnsOkWhenMessagesMarked()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<MarkConversationReadCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(3);

            var result = await _controller.MarkRead(Guid.NewGuid());

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatCloseReturnsNoContentWhenConversationClosed()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<CloseConversationCommand>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.Close(Guid.NewGuid());

            Assert.That(result, Is.TypeOf<NoContentResult>());
        }

        [Test]
        public async Task EnsureThatReopenReturnsNoContentWhenConversationReopened()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<ReopenConversationCommand>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.Reopen(Guid.NewGuid());

            Assert.That(result, Is.TypeOf<NoContentResult>());
        }
    }
}
