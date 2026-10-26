using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Models;
using App.Application.Contact.Command.SendContactEmail;
using App.Application.Contact.Queries.GetContactDetails;
using App.Application.Notifications.Dto;
using App.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace App.Test.Handlers
{
    [TestFixture]
    [Category("Contact")]
    public class ContactHandlerTest
    {
        private Mock<IContactService> _contactService = null!;
        private Mock<IIdentityService> _identityService = null!;
        private Mock<ICurrentUserService> _currentUserService = null!;
        private Mock<IEmailTemplateBuilder> _emailTemplateBuilder = null!;
        private IConfiguration _configuration = null!;
        private GetContactDetailsHandler _getContactDetailsHandler = null!;
        private SendContactEmailHandler _sendContactEmailHandler = null!;

        [SetUp]
        public void Setup()
        {
            _contactService = new Mock<IContactService>();
            _identityService = new Mock<IIdentityService>();
            _currentUserService = new Mock<ICurrentUserService>();
            _emailTemplateBuilder = new Mock<IEmailTemplateBuilder>();
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["SmtpSettings:FurniEmail"] = "furni@furni.com",
                    ["SmtpSettings:AdminEmail"] = "admin@furni.com"
                })
                .Build();

            _getContactDetailsHandler = new GetContactDetailsHandler(_identityService.Object, _currentUserService.Object, new Mock<ILogger<GetContactDetailsHandler>>().Object);
            _sendContactEmailHandler = new SendContactEmailHandler(
                _contactService.Object,
                _currentUserService.Object,
                _identityService.Object,
                _emailTemplateBuilder.Object,
                _configuration,
                new Mock<ILogger<SendContactEmailHandler>>().Object);
        }

        private static AppUser ValidUser() => new()
        {
            Id = Guid.NewGuid(),
            FName = "Ahmed",
            LName = "Ali",
            Email = "ahmedali@gmail.com",
            PhoneNumber = "01012345678"
        };

        [Test]
        public async Task EnsureGetContactDetailsHandlerReturnsDetailsWhenUserExists()
        {
            var user = ValidUser();
            _currentUserService.Setup(s => s.UserId).Returns(user.Id);
            _identityService.Setup(s => s.GetUserByIdAsync(user.Id)).ReturnsAsync(user);

            var result = await _getContactDetailsHandler.Handle(It.IsAny<GetContactDetailsQuery>(), It.IsAny<CancellationToken>());

            Assert.That(result.CustomerName, Is.EqualTo("Ahmed Ali"));
            Assert.That(result.Email, Is.EqualTo("ahmedali@gmail.com"));
            Assert.That(result.Phone, Is.EqualTo("01012345678"));
        }

        [Test]
        public void EnsureGetContactDetailsHandlerThrowsNotFoundWhenUserNotResolved()
        {
            _currentUserService.Setup(s => s.UserId).Returns((Guid?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _getContactDetailsHandler.Handle(It.IsAny<GetContactDetailsQuery>(), It.IsAny<CancellationToken>()));
        }

        [Test]
        public void EnsureGetContactDetailsHandlerThrowsNotFoundWhenUserNotExists()
        {
            var userId = Guid.NewGuid();
            _currentUserService.Setup(s => s.UserId).Returns(userId);
            _identityService.Setup(s => s.GetUserByIdAsync(userId)).ReturnsAsync((AppUser?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _getContactDetailsHandler.Handle(It.IsAny<GetContactDetailsQuery>(), It.IsAny<CancellationToken>()));
        }

        [Test]
        public async Task EnsureSendContactEmailHandlerSendsTemplatedEmailWhenUserExists()
        {
            var user = ValidUser();
            _currentUserService.Setup(s => s.UserId).Returns(user.Id);
            _identityService.Setup(s => s.GetUserByIdAsync(user.Id)).ReturnsAsync(user);
            _emailTemplateBuilder.Setup(b => b.Build(It.IsAny<EmailContent>())).Returns("<html>body</html>");
            _contactService.Setup(s => s.SendEmailServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>()))
                .ReturnsAsync(new NotifyDto { Success = true, Message = "Email sent" });

            var result = await _sendContactEmailHandler.Handle(
                new SendContactEmailCommand { Subject = "Order inquiry", Body = "Where is my order?" },
                It.IsAny<CancellationToken>());

            Assert.That(result.Success, Is.True);
            _contactService.Verify(s => s.SendEmailServiceAsync(
                "furni@furni.com",
                "admin@furni.com",
                "Order inquiry",
                "<html>body</html>",
                "Furni Support",
                "Furni Support",
                true,
                "ahmedali@gmail.com",
                "Ahmed Ali"), Times.Once);
        }

        [Test]
        public void EnsureSendContactEmailHandlerThrowsNotFoundWhenUserNotResolved()
        {
            _currentUserService.Setup(s => s.UserId).Returns((Guid?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _sendContactEmailHandler.Handle(
                    new SendContactEmailCommand { Subject = "Order inquiry", Body = "Where is my order?" },
                    It.IsAny<CancellationToken>()));
        }
    }
}
