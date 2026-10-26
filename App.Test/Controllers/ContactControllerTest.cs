using App.Application.Contact.Command.SendContactEmail;
using App.Application.Contact.Queries.GetContactDetails;
using App.Application.Notifications.Dto;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using App.Api.Controllers;

namespace App.Test.Controllers
{
    [TestFixture]
    [Category("Contact")]
    public class ContactControllerTest
    {
        private Mock<IMediator> _sender = null!;
        private ContactController _controller = null!;

        [SetUp]
        public void Setup()
        {
            _sender = new Mock<IMediator>();
            _controller = new ContactController(_sender.Object);
        }

        [Test]
        public async Task EnsureThatGetContactDetailsReturnsOkWhenDetailsExist()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<GetContactDetailsQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new App.Application.Contact.Dto.ContactDetailsDto());

            var result = await _controller.GetContactDetails();

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatSendEmailReturnsOkWhenEmailSent()
        {
            var command = new SendContactEmailCommand { Subject = "Question", Body = "I need help with my order." };
            _sender.Setup(s => s.Send(
                    It.IsAny<SendContactEmailCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new NotifyDto());

            var result = await _controller.SendEmail(command);

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }
    }
}
