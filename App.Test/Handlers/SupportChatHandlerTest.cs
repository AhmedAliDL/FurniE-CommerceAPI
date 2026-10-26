using System.Linq.Expressions;
using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Repos;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.SupportChat.Command.CloseConversation;
using App.Application.SupportChat.Command.MarkConversationRead;
using App.Application.SupportChat.Command.ReopenConversation;
using App.Application.SupportChat.Command.SendMessage;
using App.Application.SupportChat.Queries.CheckTicketAccess;
using App.Application.SupportChat.Queries.GetConversation;
using App.Application.SupportChat.Queries.GetConversationMessages;
using App.Domain.Entities;
using App.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace App.Test.Handlers
{
    [TestFixture]
    [Category("SupportChat")]
    public class SupportChatHandlerTest
    {
        private Mock<IUnitOfWork> _unitOfWork = null!;
        private Mock<IBaseRepository<SupportTicket>> _supportTickets = null!;
        private Mock<IBaseRepository<SupportConversation>> _supportConversations = null!;
        private Mock<IBaseRepository<SupportMessage>> _supportMessages = null!;
        private Mock<ICurrentUserService> _currentUserService = null!;
        private Mock<IIdentityService> _identityService = null!;
        private Mock<IRoleService> _roleService = null!;
        private Mock<ISupportService> _supportService = null!;
        private Mock<INotificationService> _notificationService = null!;

        private SendMessageHandler _sendMessageHandler = null!;
        private MarkConversationReadHandler _markConversationReadHandler = null!;
        private CloseConversationHandler _closeConversationHandler = null!;
        private ReopenConversationHandler _reopenConversationHandler = null!;
        private GetConversationHandler _getConversationHandler = null!;
        private GetConversationMessagesHandler _getConversationMessagesHandler = null!;
        private CheckTicketAccessHandler _checkTicketAccessHandler = null!;

        private readonly Guid _userId = Guid.NewGuid();

        [SetUp]
        public void Setup()
        {
            _unitOfWork = new Mock<IUnitOfWork>();
            _supportTickets = new Mock<IBaseRepository<SupportTicket>>();
            _supportConversations = new Mock<IBaseRepository<SupportConversation>>();
            _supportMessages = new Mock<IBaseRepository<SupportMessage>>();
            _currentUserService = new Mock<ICurrentUserService>();
            _identityService = new Mock<IIdentityService>();
            _roleService = new Mock<IRoleService>();
            _supportService = new Mock<ISupportService>();
            _notificationService = new Mock<INotificationService>();

            _unitOfWork.Setup(u => u.SupportTickets).Returns(_supportTickets.Object);
            _unitOfWork.Setup(u => u.SupportConversations).Returns(_supportConversations.Object);
            _unitOfWork.Setup(u => u.SupportMessages).Returns(_supportMessages.Object);

            _currentUserService.SetupGet(c => c.UserId).Returns(_userId);
            _supportService.Setup(s => s.BroadcastMessageAsync(It.IsAny<Guid>(), It.IsAny<App.Application.SupportChat.Dto.SupportMessageDto>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _supportService.Setup(s => s.NotifyMessagesReadAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _notificationService.Setup(n => n.NotifyUserAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            _sendMessageHandler = new SendMessageHandler(_unitOfWork.Object, _identityService.Object, _roleService.Object, _supportService.Object, _notificationService.Object, new Mock<ILogger<SendMessageHandler>>().Object);
            _markConversationReadHandler = new MarkConversationReadHandler(_unitOfWork.Object, _currentUserService.Object, _identityService.Object, _roleService.Object, _supportService.Object, new Mock<ILogger<MarkConversationReadHandler>>().Object);
            _closeConversationHandler = new CloseConversationHandler(_unitOfWork.Object, _currentUserService.Object, _identityService.Object, _roleService.Object, new Mock<ILogger<CloseConversationHandler>>().Object);
            _reopenConversationHandler = new ReopenConversationHandler(_unitOfWork.Object, _currentUserService.Object, _identityService.Object, _roleService.Object, new Mock<ILogger<ReopenConversationHandler>>().Object);
            _getConversationHandler = new GetConversationHandler(_unitOfWork.Object, _currentUserService.Object, _identityService.Object, _roleService.Object, new Mock<ILogger<GetConversationHandler>>().Object);
            _getConversationMessagesHandler = new GetConversationMessagesHandler(_unitOfWork.Object, _currentUserService.Object, _identityService.Object, _roleService.Object, new Mock<ILogger<GetConversationMessagesHandler>>().Object);
            _checkTicketAccessHandler = new CheckTicketAccessHandler(_unitOfWork.Object, _identityService.Object, _roleService.Object, new Mock<ILogger<CheckTicketAccessHandler>>().Object);
        }

        private SupportTicket ValidTicket() => new()
        {
            TicketId = Guid.NewGuid(),
            CustomerId = _userId,
            Subject = "Order not delivered",
            Status = SupportTicketStatus.Open,
            Priority = SupportTicketPriority.High
        };

        private SupportConversation ValidConversation(SupportConversationStatus status = SupportConversationStatus.Active) => new()
        {
            Id = Guid.NewGuid(),
            SupportTicketId = Guid.NewGuid(),
            Status = status
        };

        #region SendMessage

        [Test]
        public async Task EnsureSendMessageHandlerCreatesConversationAndMessageWhenCustomerSendsFirstMessage()
        {
            var ticket = ValidTicket();
            _supportTickets.Setup(st => st.FindAsync(It.IsAny<Expression<Func<SupportTicket, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ticket);
            _supportConversations.Setup(sc => sc.FindAsync(It.IsAny<Expression<Func<SupportConversation, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SupportConversation?)null);
            _identityService.Setup(i => i.GetUserByIdAsync(_userId))
                .ReturnsAsync(new AppUser { Id = _userId, FName = "Ahmed", LName = "Ali" });

            var result = await _sendMessageHandler.Handle(new SendMessageCommand(ticket.TicketId, _userId, "Where is my order?"), It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.TicketId, Is.EqualTo(ticket.TicketId));
                Assert.That(result.SenderId, Is.EqualTo(_userId));
                Assert.That(result.SenderName, Is.EqualTo("Ahmed Ali"));
                Assert.That(result.Content, Is.EqualTo("Where is my order?"));
                Assert.That(result.IsRead, Is.False);
            });
            _supportConversations.Verify(sc => sc.AddAsync(It.Is<SupportConversation>(c =>
                c.SupportTicketId == ticket.TicketId &&
                c.Status == SupportConversationStatus.Active), It.IsAny<CancellationToken>()), Times.Once);
            _supportMessages.Verify(sm => sm.AddAsync(It.Is<SupportMessage>(m =>
                m.SenderId == _userId &&
                m.Content == "Where is my order?" &&
                m.IsRead == false), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
            _supportService.Verify(s => s.BroadcastMessageAsync(ticket.TicketId, It.IsAny<App.Application.SupportChat.Dto.SupportMessageDto>(), It.IsAny<CancellationToken>()), Times.Once);
            _notificationService.Verify(n => n.NotifyUserAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task EnsureSendMessageHandlerReusesExistingConversationWhenCustomerReplies()
        {
            var ticket = ValidTicket();
            var conversation = ValidConversation();
            conversation.SupportTicketId = ticket.TicketId;
            _supportTickets.Setup(st => st.FindAsync(It.IsAny<Expression<Func<SupportTicket, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ticket);
            _supportConversations.Setup(sc => sc.FindAsync(It.IsAny<Expression<Func<SupportConversation, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(conversation);
            _identityService.Setup(i => i.GetUserByIdAsync(_userId))
                .ReturnsAsync(new AppUser { Id = _userId, FName = "Ahmed", LName = "Ali" });

            await _sendMessageHandler.Handle(new SendMessageCommand(ticket.TicketId, _userId, "Still waiting"), It.IsAny<CancellationToken>());

            _supportConversations.Verify(sc => sc.AddAsync(It.IsAny<SupportConversation>(), It.IsAny<CancellationToken>()), Times.Never);
            _supportMessages.Verify(sm => sm.AddAsync(It.Is<SupportMessage>(m => m.ConversationId == conversation.Id), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task EnsureSendMessageHandlerReopensClosedConversationWhenCustomerSendsMessage()
        {
            var ticket = ValidTicket();
            var conversation = ValidConversation(SupportConversationStatus.Closed);
            conversation.SupportTicketId = ticket.TicketId;
            _supportTickets.Setup(st => st.FindAsync(It.IsAny<Expression<Func<SupportTicket, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ticket);
            _supportConversations.Setup(sc => sc.FindAsync(It.IsAny<Expression<Func<SupportConversation, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(conversation);
            _identityService.Setup(i => i.GetUserByIdAsync(_userId))
                .ReturnsAsync(new AppUser { Id = _userId, FName = "Ahmed", LName = "Ali" });

            await _sendMessageHandler.Handle(new SendMessageCommand(ticket.TicketId, _userId, "Please reopen"), It.IsAny<CancellationToken>());

            _supportConversations.Verify(sc => sc.Update(It.Is<SupportConversation>(c =>
                c.Status == SupportConversationStatus.Active &&
                c.ClosedAt == null)), Times.Once);
        }

        [Test]
        public async Task EnsureSendMessageHandlerSendsMessageAndNotifiesCustomerWhenUserIsAgent()
        {
            var agentId = Guid.NewGuid();
            var ticket = ValidTicket();
            var conversation = ValidConversation();
            conversation.SupportTicketId = ticket.TicketId;
            _supportTickets.Setup(st => st.FindAsync(It.IsAny<Expression<Func<SupportTicket, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ticket);
            _supportConversations.Setup(sc => sc.FindAsync(It.IsAny<Expression<Func<SupportConversation, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(conversation);

            var agent = new AppUser { Id = agentId, FName = "Support", LName = "Agent" };
            _identityService.Setup(i => i.GetUserByIdAsync(agentId)).ReturnsAsync(agent);
            _roleService.Setup(r => r.GetAllRolesOfUser(agent)).ReturnsAsync(new List<string> { "SupportAgent" });

            var result = await _sendMessageHandler.Handle(new SendMessageCommand(ticket.TicketId, agentId, "We are checking your order"), It.IsAny<CancellationToken>());

            Assert.That(result.SenderName, Is.EqualTo("Support Agent"));
            _supportMessages.Verify(sm => sm.AddAsync(It.Is<SupportMessage>(m => m.SenderId == agentId), It.IsAny<CancellationToken>()), Times.Once);
            _notificationService.Verify(n => n.NotifyUserAsync(ticket.CustomerId, It.IsAny<string>(), agentId, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void EnsureSendMessageHandlerThrowsForbiddenWhenAgentRepliesToClosedConversation()
        {
            var agentId = Guid.NewGuid();
            var ticket = ValidTicket();
            var conversation = ValidConversation(SupportConversationStatus.Closed);
            conversation.SupportTicketId = ticket.TicketId;
            _supportTickets.Setup(st => st.FindAsync(It.IsAny<Expression<Func<SupportTicket, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ticket);
            _supportConversations.Setup(sc => sc.FindAsync(It.IsAny<Expression<Func<SupportConversation, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(conversation);

            var agent = new AppUser { Id = agentId, FName = "Support", LName = "Agent" };
            _identityService.Setup(i => i.GetUserByIdAsync(agentId)).ReturnsAsync(agent);
            _roleService.Setup(r => r.GetAllRolesOfUser(agent)).ReturnsAsync(new List<string> { "SupportAgent" });

            Assert.ThrowsAsync<ForbiddenException>(async () =>
                await _sendMessageHandler.Handle(new SendMessageCommand(ticket.TicketId, agentId, "Too late"), It.IsAny<CancellationToken>()));
        }

        [Test]
        public void EnsureSendMessageHandlerThrowsForbiddenWhenSenderIsNeitherOwnerNorStaff()
        {
            var otherUserId = Guid.NewGuid();
            var ticket = ValidTicket();
            _supportTickets.Setup(st => st.FindAsync(It.IsAny<Expression<Func<SupportTicket, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ticket);

            var otherUser = new AppUser { Id = otherUserId, FName = "Another", LName = "Customer" };
            _identityService.Setup(i => i.GetUserByIdAsync(otherUserId)).ReturnsAsync(otherUser);
            _roleService.Setup(r => r.GetAllRolesOfUser(otherUser)).ReturnsAsync(new List<string> { "Customer" });

            Assert.ThrowsAsync<ForbiddenException>(async () =>
                await _sendMessageHandler.Handle(new SendMessageCommand(ticket.TicketId, otherUserId, "Hi"), It.IsAny<CancellationToken>()));
        }

        [Test]
        public void EnsureSendMessageHandlerThrowsNotFoundWhenTicketNotFound()
        {
            _supportTickets.Setup(st => st.FindAsync(It.IsAny<Expression<Func<SupportTicket, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SupportTicket?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _sendMessageHandler.Handle(new SendMessageCommand(Guid.NewGuid(), _userId, "Hi"), It.IsAny<CancellationToken>()));
        }

        #endregion

        #region MarkConversationRead

        [Test]
        public async Task EnsureMarkConversationReadHandlerMarksUnreadMessagesAsRead()
        {
            var ticket = ValidTicket();
            var conversation = ValidConversation();
            conversation.SupportTicketId = ticket.TicketId;
            _supportTickets.Setup(st => st.FindAsync(It.IsAny<Expression<Func<SupportTicket, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ticket);
            _supportConversations.Setup(sc => sc.FindAsync(It.IsAny<Expression<Func<SupportConversation, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(conversation);

            var unread = new List<SupportMessage>
            {
                new() { ConversationId = conversation.Id, SenderId = Guid.NewGuid(), Content = "We are on it", IsRead = false },
                new() { ConversationId = conversation.Id, SenderId = Guid.NewGuid(), Content = "Any update?", IsRead = false }
            };
            _supportMessages.Setup(sm => sm.FindAllAsync(It.IsAny<Expression<Func<SupportMessage, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(unread);
            _unitOfWork.Setup(u => u.CompleteAsync(It.IsAny<CancellationToken>())).ReturnsAsync(2);

            var result = await _markConversationReadHandler.Handle(new MarkConversationReadCommand(ticket.TicketId), It.IsAny<CancellationToken>());

            Assert.That(result, Is.EqualTo(2));
            _supportMessages.Verify(sm => sm.UpdateRange(It.Is<List<SupportMessage>>(list =>
                list.Count == 2 &&
                list.All(m => m.IsRead && m.ReadAt != null))), Times.Once);
            _supportService.Verify(s => s.NotifyMessagesReadAsync(ticket.TicketId, _userId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void EnsureMarkConversationReadHandlerThrowsForbiddenWhenUserIsNotOwnerOrStaff()
        {
            var ticket = ValidTicket();
            ticket.CustomerId = Guid.NewGuid();
            _supportTickets.Setup(st => st.FindAsync(It.IsAny<Expression<Func<SupportTicket, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ticket);

            var user = new AppUser { Id = _userId, FName = "Another", LName = "Customer" };
            _identityService.Setup(i => i.GetUserByIdAsync(_userId)).ReturnsAsync(user);
            _roleService.Setup(r => r.GetAllRolesOfUser(user)).ReturnsAsync(new List<string> { "Customer" });

            Assert.ThrowsAsync<ForbiddenException>(async () =>
                await _markConversationReadHandler.Handle(new MarkConversationReadCommand(ticket.TicketId), It.IsAny<CancellationToken>()));
        }

        [Test]
        public void EnsureMarkConversationReadHandlerThrowsNotFoundWhenNoConversationExists()
        {
            var ticket = ValidTicket();
            _supportTickets.Setup(st => st.FindAsync(It.IsAny<Expression<Func<SupportTicket, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ticket);
            _supportConversations.Setup(sc => sc.FindAsync(It.IsAny<Expression<Func<SupportConversation, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SupportConversation?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _markConversationReadHandler.Handle(new MarkConversationReadCommand(ticket.TicketId), It.IsAny<CancellationToken>()));
        }

        #endregion

        #region CloseConversation

        [Test]
        public async Task EnsureCloseConversationHandlerClosesActiveConversationWhenUserIsAgent()
        {
            var ticket = ValidTicket();
            var conversation = ValidConversation();
            conversation.SupportTicketId = ticket.TicketId;
            _supportTickets.Setup(st => st.FindAsync(It.IsAny<Expression<Func<SupportTicket, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ticket);
            _supportConversations.Setup(sc => sc.FindAsync(It.IsAny<Expression<Func<SupportConversation, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(conversation);

            var user = new AppUser { Id = _userId, FName = "Support", LName = "Agent" };
            _identityService.Setup(i => i.GetUserByIdAsync(_userId)).ReturnsAsync(user);
            _roleService.Setup(r => r.GetAllRolesOfUser(user)).ReturnsAsync(new List<string> { "SupportAgent" });

            await _closeConversationHandler.Handle(new CloseConversationCommand(ticket.TicketId), It.IsAny<CancellationToken>());

            _supportConversations.Verify(sc => sc.Update(It.Is<SupportConversation>(c =>
                c.Status == SupportConversationStatus.Closed &&
                c.ClosedAt != null)), Times.Once);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task EnsureCloseConversationHandlerSkipsUpdateWhenConversationIsAlreadyClosed()
        {
            var ticket = ValidTicket();
            var conversation = ValidConversation(SupportConversationStatus.Closed);
            conversation.SupportTicketId = ticket.TicketId;
            _supportTickets.Setup(st => st.FindAsync(It.IsAny<Expression<Func<SupportTicket, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ticket);
            _supportConversations.Setup(sc => sc.FindAsync(It.IsAny<Expression<Func<SupportConversation, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(conversation);

            var user = new AppUser { Id = _userId, FName = "Support", LName = "Agent" };
            _identityService.Setup(i => i.GetUserByIdAsync(_userId)).ReturnsAsync(user);
            _roleService.Setup(r => r.GetAllRolesOfUser(user)).ReturnsAsync(new List<string> { "Admin" });

            await _closeConversationHandler.Handle(new CloseConversationCommand(ticket.TicketId), It.IsAny<CancellationToken>());

            _supportConversations.Verify(sc => sc.Update(It.IsAny<SupportConversation>()), Times.Never);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public void EnsureCloseConversationHandlerThrowsForbiddenWhenUserIsNotStaff()
        {
            var ticket = ValidTicket();
            _supportTickets.Setup(st => st.FindAsync(It.IsAny<Expression<Func<SupportTicket, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ticket);

            var user = new AppUser { Id = _userId, FName = "Ahmed", LName = "Ali" };
            _identityService.Setup(i => i.GetUserByIdAsync(_userId)).ReturnsAsync(user);
            _roleService.Setup(r => r.GetAllRolesOfUser(user)).ReturnsAsync(new List<string> { "Customer" });

            Assert.ThrowsAsync<ForbiddenException>(async () =>
                await _closeConversationHandler.Handle(new CloseConversationCommand(ticket.TicketId), It.IsAny<CancellationToken>()));
        }

        [Test]
        public void EnsureCloseConversationHandlerThrowsNotFoundWhenNoConversationExists()
        {
            var ticket = ValidTicket();
            _supportTickets.Setup(st => st.FindAsync(It.IsAny<Expression<Func<SupportTicket, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ticket);
            _supportConversations.Setup(sc => sc.FindAsync(It.IsAny<Expression<Func<SupportConversation, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SupportConversation?)null);

            var user = new AppUser { Id = _userId, FName = "Support", LName = "Agent" };
            _identityService.Setup(i => i.GetUserByIdAsync(_userId)).ReturnsAsync(user);
            _roleService.Setup(r => r.GetAllRolesOfUser(user)).ReturnsAsync(new List<string> { "SupportAgent" });

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _closeConversationHandler.Handle(new CloseConversationCommand(ticket.TicketId), It.IsAny<CancellationToken>()));
        }

        #endregion

        #region ReopenConversation

        [Test]
        public async Task EnsureReopenConversationHandlerReopensClosedConversationWhenUserIsOwner()
        {
            var ticket = ValidTicket();
            var conversation = ValidConversation(SupportConversationStatus.Closed);
            conversation.SupportTicketId = ticket.TicketId;
            _supportTickets.Setup(st => st.FindAsync(It.IsAny<Expression<Func<SupportTicket, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ticket);
            _supportConversations.Setup(sc => sc.FindAsync(It.IsAny<Expression<Func<SupportConversation, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(conversation);

            await _reopenConversationHandler.Handle(new ReopenConversationCommand(ticket.TicketId), It.IsAny<CancellationToken>());

            _supportConversations.Verify(sc => sc.Update(It.Is<SupportConversation>(c =>
                c.Status == SupportConversationStatus.Active &&
                c.ClosedAt == null)), Times.Once);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void EnsureReopenConversationHandlerThrowsForbiddenWhenUserIsNotOwnerOrStaff()
        {
            var ticket = ValidTicket();
            ticket.CustomerId = Guid.NewGuid();
            _supportTickets.Setup(st => st.FindAsync(It.IsAny<Expression<Func<SupportTicket, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ticket);

            var user = new AppUser { Id = _userId, FName = "Another", LName = "Customer" };
            _identityService.Setup(i => i.GetUserByIdAsync(_userId)).ReturnsAsync(user);
            _roleService.Setup(r => r.GetAllRolesOfUser(user)).ReturnsAsync(new List<string> { "Customer" });

            Assert.ThrowsAsync<ForbiddenException>(async () =>
                await _reopenConversationHandler.Handle(new ReopenConversationCommand(ticket.TicketId), It.IsAny<CancellationToken>()));
        }

        [Test]
        public void EnsureReopenConversationHandlerThrowsNotFoundWhenNoConversationExists()
        {
            var ticket = ValidTicket();
            _supportTickets.Setup(st => st.FindAsync(It.IsAny<Expression<Func<SupportTicket, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ticket);
            _supportConversations.Setup(sc => sc.FindAsync(It.IsAny<Expression<Func<SupportConversation, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SupportConversation?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _reopenConversationHandler.Handle(new ReopenConversationCommand(ticket.TicketId), It.IsAny<CancellationToken>()));
        }

        #endregion

        #region GetConversation

        [Test]
        public async Task EnsureGetConversationHandlerReturnsConversationWhenUserIsOwner()
        {
            var ticket = ValidTicket();
            var conversation = ValidConversation();
            conversation.SupportTicketId = ticket.TicketId;
            _supportTickets.Setup(st => st.FindAsync(It.IsAny<Expression<Func<SupportTicket, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ticket);
            _supportConversations.Setup(sc => sc.FindAsync(It.IsAny<Expression<Func<SupportConversation, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(conversation);

            var result = await _getConversationHandler.Handle(new GetConversationQuery(ticket.TicketId), It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.ConversationId, Is.EqualTo(conversation.Id));
                Assert.That(result.TicketId, Is.EqualTo(ticket.TicketId));
                Assert.That(result.Status, Is.EqualTo("Active"));
                Assert.That(result.ClosedAt, Is.Null);
            });
            _identityService.Verify(i => i.GetUserByIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Test]
        public async Task EnsureGetConversationHandlerReturnsConversationWhenUserIsSupportStaff()
        {
            var ticket = ValidTicket();
            ticket.CustomerId = Guid.NewGuid();
            var conversation = ValidConversation(SupportConversationStatus.Closed);
            conversation.SupportTicketId = ticket.TicketId;
            _supportTickets.Setup(st => st.FindAsync(It.IsAny<Expression<Func<SupportTicket, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ticket);
            _supportConversations.Setup(sc => sc.FindAsync(It.IsAny<Expression<Func<SupportConversation, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(conversation);

            var user = new AppUser { Id = _userId, FName = "Support", LName = "Agent" };
            _identityService.Setup(i => i.GetUserByIdAsync(_userId)).ReturnsAsync(user);
            _roleService.Setup(r => r.GetAllRolesOfUser(user)).ReturnsAsync(new List<string> { "SupportAgent" });

            var result = await _getConversationHandler.Handle(new GetConversationQuery(ticket.TicketId), It.IsAny<CancellationToken>());

            Assert.That(result.Status, Is.EqualTo("Closed"));
        }

        [Test]
        public void EnsureGetConversationHandlerThrowsForbiddenWhenUserIsNeitherOwnerNorStaff()
        {
            var ticket = ValidTicket();
            ticket.CustomerId = Guid.NewGuid();
            _supportTickets.Setup(st => st.FindAsync(It.IsAny<Expression<Func<SupportTicket, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ticket);

            var user = new AppUser { Id = _userId, FName = "Another", LName = "Customer" };
            _identityService.Setup(i => i.GetUserByIdAsync(_userId)).ReturnsAsync(user);
            _roleService.Setup(r => r.GetAllRolesOfUser(user)).ReturnsAsync(new List<string> { "Customer" });

            Assert.ThrowsAsync<ForbiddenException>(async () =>
                await _getConversationHandler.Handle(new GetConversationQuery(ticket.TicketId), It.IsAny<CancellationToken>()));
        }

        [Test]
        public void EnsureGetConversationHandlerThrowsNotFoundWhenNoConversationExists()
        {
            var ticket = ValidTicket();
            _supportTickets.Setup(st => st.FindAsync(It.IsAny<Expression<Func<SupportTicket, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ticket);
            _supportConversations.Setup(sc => sc.FindAsync(It.IsAny<Expression<Func<SupportConversation, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SupportConversation?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _getConversationHandler.Handle(new GetConversationQuery(ticket.TicketId), It.IsAny<CancellationToken>()));
        }

        #endregion

        #region GetConversationMessages

        [Test]
        public async Task EnsureGetConversationMessagesHandlerReturnsMessagesForOwner()
        {
            var ticket = ValidTicket();
            var conversation = ValidConversation();
            conversation.SupportTicketId = ticket.TicketId;
            _supportTickets.Setup(st => st.FindAsync(It.IsAny<Expression<Func<SupportTicket, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ticket);
            _supportConversations.Setup(sc => sc.FindAsync(It.IsAny<Expression<Func<SupportConversation, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(conversation);
            _supportMessages.Setup(sm => sm.FindPagedAsync(
                    It.IsAny<Expression<Func<SupportMessage, bool>>>(),
                    It.IsAny<Expression<Func<SupportMessage, object>>[]>(),
                    It.IsAny<App.Application.Common.Enums.SortDirection>(),
                    It.IsAny<Expression<Func<SupportMessage, object>>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(((IReadOnlyList<SupportMessage>)
                [
                    new() { Id = Guid.NewGuid(), ConversationId = conversation.Id, SenderId = _userId, Content = "Where is my order?", IsRead = false, Sender = new AppUser { Id = _userId, FName = "Ahmed", LName = "Ali" } }
                ], 1));

            var result = await _getConversationMessagesHandler.Handle(new GetConversationMessagesQuery(ticket.TicketId), It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.TotalCount, Is.EqualTo(1));
                Assert.That(result.Items, Has.Count.EqualTo(1));
                Assert.That(result.Items[0].TicketId, Is.EqualTo(ticket.TicketId));
                Assert.That(result.Items[0].SenderId, Is.EqualTo(_userId));
                Assert.That(result.Items[0].SenderName, Is.EqualTo("Ahmed Ali"));
                Assert.That(result.Items[0].Content, Is.EqualTo("Where is my order?"));
                Assert.That(result.Items[0].IsRead, Is.False);
            });
        }

        [Test]
        public void EnsureGetConversationMessagesHandlerThrowsForbiddenWhenUserIsNeitherOwnerNorStaff()
        {
            var ticket = ValidTicket();
            ticket.CustomerId = Guid.NewGuid();
            _supportTickets.Setup(st => st.FindAsync(It.IsAny<Expression<Func<SupportTicket, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ticket);

            var user = new AppUser { Id = _userId, FName = "Another", LName = "Customer" };
            _identityService.Setup(i => i.GetUserByIdAsync(_userId)).ReturnsAsync(user);
            _roleService.Setup(r => r.GetAllRolesOfUser(user)).ReturnsAsync(new List<string> { "Customer" });

            Assert.ThrowsAsync<ForbiddenException>(async () =>
                await _getConversationMessagesHandler.Handle(new GetConversationMessagesQuery(ticket.TicketId), It.IsAny<CancellationToken>()));
        }

        #endregion

        #region CheckTicketAccess

        [Test]
        public void EnsureCheckTicketAccessHandlerPassesWhenUserIsOwner()
        {
            var ticket = ValidTicket();
            _supportTickets.Setup(st => st.FindAsync(It.IsAny<Expression<Func<SupportTicket, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ticket);

            Assert.DoesNotThrowAsync(async () =>
                await _checkTicketAccessHandler.Handle(new CheckTicketAccessQuery(ticket.TicketId, _userId), It.IsAny<CancellationToken>()));
        }

        [Test]
        public void EnsureCheckTicketAccessHandlerPassesWhenUserIsSupportStaff()
        {
            var ticket = ValidTicket();
            ticket.CustomerId = Guid.NewGuid();
            _supportTickets.Setup(st => st.FindAsync(It.IsAny<Expression<Func<SupportTicket, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ticket);

            var user = new AppUser { Id = _userId, FName = "Support", LName = "Agent" };
            _identityService.Setup(i => i.GetUserByIdAsync(_userId)).ReturnsAsync(user);
            _roleService.Setup(r => r.GetAllRolesOfUser(user)).ReturnsAsync(new List<string> { "SupportAgent" });

            Assert.DoesNotThrowAsync(async () =>
                await _checkTicketAccessHandler.Handle(new CheckTicketAccessQuery(ticket.TicketId, _userId), It.IsAny<CancellationToken>()));
        }

        [Test]
        public void EnsureCheckTicketAccessHandlerThrowsForbiddenWhenUserIsNeitherOwnerNorStaff()
        {
            var ticket = ValidTicket();
            ticket.CustomerId = Guid.NewGuid();
            _supportTickets.Setup(st => st.FindAsync(It.IsAny<Expression<Func<SupportTicket, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ticket);

            var user = new AppUser { Id = _userId, FName = "Another", LName = "Customer" };
            _identityService.Setup(i => i.GetUserByIdAsync(_userId)).ReturnsAsync(user);
            _roleService.Setup(r => r.GetAllRolesOfUser(user)).ReturnsAsync(new List<string> { "Customer" });

            Assert.ThrowsAsync<ForbiddenException>(async () =>
                await _checkTicketAccessHandler.Handle(new CheckTicketAccessQuery(ticket.TicketId, _userId), It.IsAny<CancellationToken>()));
        }

        [Test]
        public void EnsureCheckTicketAccessHandlerThrowsNotFoundWhenTicketNotFound()
        {
            _supportTickets.Setup(st => st.FindAsync(It.IsAny<Expression<Func<SupportTicket, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SupportTicket?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _checkTicketAccessHandler.Handle(new CheckTicketAccessQuery(Guid.NewGuid(), _userId), It.IsAny<CancellationToken>()));
        }

        #endregion
    }
}
