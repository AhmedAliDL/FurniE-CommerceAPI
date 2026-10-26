using System.Linq.Expressions;
using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Repos;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.SupportTickets.Command.CreateSupportTicket;
using App.Application.SupportTickets.Command.UpdateTicketStatus;
using App.Application.SupportTickets.Queries.GetAllTickets;
using App.Application.SupportTickets.Queries.GetMySupportTickets;
using App.Application.SupportTickets.Queries.GetSupportTicketById;
using App.Domain.Entities;
using App.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace App.Test.Handlers
{
    [TestFixture]
    [Category("SupportTickets")]
    public class SupportTicketsHandlerTest
    {
        private Mock<IUnitOfWork> _unitOfWork = null!;
        private Mock<IBaseRepository<SupportTicket>> _supportTickets = null!;
        private Mock<IBaseRepository<SupportConversation>> _supportConversations = null!;
        private Mock<IBaseRepository<SupportMessage>> _supportMessages = null!;
        private Mock<ICurrentUserService> _currentUserService = null!;
        private Mock<IIdentityService> _identityService = null!;
        private Mock<IRoleService> _roleService = null!;

        private CreateSupportTicketHandler _createSupportTicketHandler = null!;
        private UpdateTicketStatusHandler _updateTicketStatusHandler = null!;
        private GetMySupportTicketsHandler _getMySupportTicketsHandler = null!;
        private GetAllTicketsHandler _getAllTicketsHandler = null!;
        private GetSupportTicketByIdHandler _getSupportTicketByIdHandler = null!;

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

            _unitOfWork.Setup(u => u.SupportTickets).Returns(_supportTickets.Object);
            _unitOfWork.Setup(u => u.SupportConversations).Returns(_supportConversations.Object);
            _unitOfWork.Setup(u => u.SupportMessages).Returns(_supportMessages.Object);

            _currentUserService.SetupGet(c => c.UserId).Returns(_userId);

            _createSupportTicketHandler = new CreateSupportTicketHandler(_unitOfWork.Object, _currentUserService.Object, new Mock<ILogger<CreateSupportTicketHandler>>().Object);
            _updateTicketStatusHandler = new UpdateTicketStatusHandler(_unitOfWork.Object, new Mock<ILogger<UpdateTicketStatusHandler>>().Object);
            _getMySupportTicketsHandler = new GetMySupportTicketsHandler(_unitOfWork.Object, _currentUserService.Object, new Mock<ILogger<GetMySupportTicketsHandler>>().Object);
            _getAllTicketsHandler = new GetAllTicketsHandler(_unitOfWork.Object, new Mock<ILogger<GetAllTicketsHandler>>().Object);
            _getSupportTicketByIdHandler = new GetSupportTicketByIdHandler(_unitOfWork.Object, _currentUserService.Object, _identityService.Object, _roleService.Object, new Mock<ILogger<GetSupportTicketByIdHandler>>().Object);
        }

        private SupportTicket ValidTicket(SupportTicketStatus status = SupportTicketStatus.Open) => new()
        {
            TicketId = Guid.NewGuid(),
            CustomerId = _userId,
            Subject = "Order not delivered",
            Status = status,
            Priority = SupportTicketPriority.High
        };

        [Test]
        public async Task EnsureCreateSupportTicketHandlerCreatesTicketWhenUserIsValid()
        {
            var result = await _createSupportTicketHandler.Handle(new CreateSupportTicketCommand("Order not delivered", SupportTicketPriority.High), It.IsAny<CancellationToken>());

            _supportTickets.Verify(st => st.AddAsync(It.Is<SupportTicket>(ticket =>
                ticket.CustomerId == _userId &&
                ticket.Subject == "Order not delivered" &&
                ticket.Priority == SupportTicketPriority.High &&
                ticket.Status == SupportTicketStatus.Open), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
            Assert.That(result, Is.TypeOf<Guid>());
        }

        [Test]
        public void EnsureCreateSupportTicketHandlerThrowsNotFoundWhenUserIsNull()
        {
            _currentUserService.SetupGet(c => c.UserId).Returns((Guid?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _createSupportTicketHandler.Handle(new CreateSupportTicketCommand("Order not delivered", SupportTicketPriority.High), It.IsAny<CancellationToken>()));
        }

        [Test]
        public async Task EnsureUpdateTicketStatusHandlerUpdatesStatusWhenTicketIsOpen()
        {
            var ticket = ValidTicket();
            _supportTickets.Setup(st => st.FindAsync(It.IsAny<Expression<Func<SupportTicket, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ticket);

            await _updateTicketStatusHandler.Handle(new UpdateTicketStatusCommand(ticket.TicketId, SupportTicketStatus.InProgress), It.IsAny<CancellationToken>());

            _supportTickets.Verify(st => st.Update(It.Is<SupportTicket>(t =>
                t.Status == SupportTicketStatus.InProgress &&
                t.UpdatedAt != null &&
                t.ClosedAt == null)), Times.Once);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task EnsureUpdateTicketStatusHandlerSetsClosedAtWhenTicketIsClosed()
        {
            var ticket = ValidTicket();
            _supportTickets.Setup(st => st.FindAsync(It.IsAny<Expression<Func<SupportTicket, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ticket);

            await _updateTicketStatusHandler.Handle(new UpdateTicketStatusCommand(ticket.TicketId, SupportTicketStatus.Closed), It.IsAny<CancellationToken>());

            _supportTickets.Verify(st => st.Update(It.Is<SupportTicket>(t =>
                t.Status == SupportTicketStatus.Closed &&
                t.ClosedAt != null)), Times.Once);
        }

        [Test]
        public void EnsureUpdateTicketStatusHandlerThrowsConflictWhenClosedTicketIsNotReopenedToOpen()
        {
            var ticket = ValidTicket(SupportTicketStatus.Closed);
            _supportTickets.Setup(st => st.FindAsync(It.IsAny<Expression<Func<SupportTicket, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ticket);

            Assert.ThrowsAsync<ConflictException>(async () =>
                await _updateTicketStatusHandler.Handle(new UpdateTicketStatusCommand(ticket.TicketId, SupportTicketStatus.InProgress), It.IsAny<CancellationToken>()));
        }

        [Test]
        public async Task EnsureUpdateTicketStatusHandlerReopensClosedTicketWhenStatusIsOpen()
        {
            var ticket = ValidTicket(SupportTicketStatus.Closed);
            ticket.ClosedAt = DateTime.UtcNow;
            _supportTickets.Setup(st => st.FindAsync(It.IsAny<Expression<Func<SupportTicket, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ticket);

            await _updateTicketStatusHandler.Handle(new UpdateTicketStatusCommand(ticket.TicketId, SupportTicketStatus.Open), It.IsAny<CancellationToken>());

            _supportTickets.Verify(st => st.Update(It.Is<SupportTicket>(t =>
                t.Status == SupportTicketStatus.Open &&
                t.ClosedAt == null)), Times.Once);
            _unitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void EnsureUpdateTicketStatusHandlerThrowsNotFoundWhenTicketNotFound()
        {
            _supportTickets.Setup(st => st.FindAsync(It.IsAny<Expression<Func<SupportTicket, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SupportTicket?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _updateTicketStatusHandler.Handle(new UpdateTicketStatusCommand(Guid.NewGuid(), SupportTicketStatus.InProgress), It.IsAny<CancellationToken>()));
        }

        [Test]
        public async Task EnsureGetMySupportTicketsHandlerReturnsTicketsForCurrentUser()
        {
            var ticket = ValidTicket();
            _supportTickets.Setup(st => st.FindPagedAsync(
                    It.IsAny<Expression<Func<SupportTicket, bool>>>(),
                    It.IsAny<Expression<Func<SupportTicket, object>>[]>(),
                    It.IsAny<App.Application.Common.Enums.SortDirection>(),
                    It.IsAny<Expression<Func<SupportTicket, object>>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(((IReadOnlyList<SupportTicket>)[ticket], 1));

            var result = await _getMySupportTicketsHandler.Handle(new GetMySupportTicketsQuery(), It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.TotalCount, Is.EqualTo(1));
                Assert.That(result.Items, Has.Count.EqualTo(1));
                Assert.That(result.Items[0].TicketId, Is.EqualTo(ticket.TicketId));
                Assert.That(result.Items[0].Subject, Is.EqualTo("Order not delivered"));
                Assert.That(result.Items[0].Status, Is.EqualTo("Open"));
                Assert.That(result.Items[0].Priority, Is.EqualTo("High"));
            });
        }

        [Test]
        public void EnsureGetMySupportTicketsHandlerThrowsNotFoundWhenUserIsNull()
        {
            _currentUserService.SetupGet(c => c.UserId).Returns((Guid?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _getMySupportTicketsHandler.Handle(new GetMySupportTicketsQuery(), It.IsAny<CancellationToken>()));
        }

        [Test]
        public async Task EnsureGetAllTicketsHandlerReturnsTicketsWithUnreadCustomerMessages()
        {
            var ticket = ValidTicket();
            ticket.Customer = new AppUser { Id = _userId, FName = "Ahmed", LName = "Ali" };
            _supportTickets.Setup(st => st.FindPagedAsync(
                    It.IsAny<Expression<Func<SupportTicket, bool>>>(),
                    It.IsAny<Expression<Func<SupportTicket, object>>[]>(),
                    It.IsAny<App.Application.Common.Enums.SortDirection>(),
                    It.IsAny<Expression<Func<SupportTicket, object>>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(((IReadOnlyList<SupportTicket>)[ticket], 1));

            var conversationId = Guid.NewGuid();
            _supportConversations.Setup(sc => sc.FindAllAsync(It.IsAny<Expression<Func<SupportConversation, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<SupportConversation> { new() { Id = conversationId, SupportTicketId = ticket.TicketId, Status = SupportConversationStatus.Active } });
            _supportMessages.Setup(sm => sm.FindAllAsync(It.IsAny<Expression<Func<SupportMessage, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<SupportMessage>
                {
                    new() { ConversationId = conversationId, SenderId = _userId, Content = "Where is my order?", IsRead = false }
                });

            var result = await _getAllTicketsHandler.Handle(new GetAllTicketsQuery(), It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.TotalCount, Is.EqualTo(1));
                Assert.That(result.Items, Has.Count.EqualTo(1));
                Assert.That(result.Items[0].TicketId, Is.EqualTo(ticket.TicketId));
                Assert.That(result.Items[0].TicketStatus, Is.EqualTo("Open"));
                Assert.That(result.Items[0].Priority, Is.EqualTo("High"));
                Assert.That(result.Items[0].CustomerName, Is.EqualTo("Ahmed Ali"));
                Assert.That(result.Items[0].ConversationStatus, Is.EqualTo("Active"));
                Assert.That(result.Items[0].LastMessagePreview, Is.EqualTo("Where is my order?"));
                Assert.That(result.Items[0].UnreadCount, Is.EqualTo(1));
            });
        }

        [Test]
        public async Task EnsureGetAllTicketsHandlerIgnoresUnreadStaffMessagesWhenCountingUnread()
        {
            var ticket = ValidTicket();
            ticket.Customer = new AppUser { Id = _userId, FName = "Ahmed", LName = "Ali" };
            _supportTickets.Setup(st => st.FindPagedAsync(
                    It.IsAny<Expression<Func<SupportTicket, bool>>>(),
                    It.IsAny<Expression<Func<SupportTicket, object>>[]>(),
                    It.IsAny<App.Application.Common.Enums.SortDirection>(),
                    It.IsAny<Expression<Func<SupportTicket, object>>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(((IReadOnlyList<SupportTicket>)[ticket], 1));

            var conversationId = Guid.NewGuid();
            _supportConversations.Setup(sc => sc.FindAllAsync(It.IsAny<Expression<Func<SupportConversation, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<SupportConversation> { new() { Id = conversationId, SupportTicketId = ticket.TicketId } });
            _supportMessages.Setup(sm => sm.FindAllAsync(It.IsAny<Expression<Func<SupportMessage, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<SupportMessage>
                {
                    new() { ConversationId = conversationId, SenderId = Guid.NewGuid(), Content = "We are on it", IsRead = false }
                });

            var result = await _getAllTicketsHandler.Handle(new GetAllTicketsQuery(), It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.Items[0].UnreadCount, Is.EqualTo(0));
                Assert.That(result.Items[0].LastMessagePreview, Is.EqualTo("We are on it"));
            });
        }

        [Test]
        public async Task EnsureGetAllTicketsHandlerReturnsEmptyListWhenNoConversationsExist()
        {
            var ticket = ValidTicket();
            ticket.Customer = new AppUser { Id = _userId, FName = "Ahmed", LName = "Ali" };
            _supportTickets.Setup(st => st.FindPagedAsync(
                    It.IsAny<Expression<Func<SupportTicket, bool>>>(),
                    It.IsAny<Expression<Func<SupportTicket, object>>[]>(),
                    It.IsAny<App.Application.Common.Enums.SortDirection>(),
                    It.IsAny<Expression<Func<SupportTicket, object>>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(((IReadOnlyList<SupportTicket>)[ticket], 1));
            _supportConversations.Setup(sc => sc.FindAllAsync(It.IsAny<Expression<Func<SupportConversation, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<SupportConversation>());
            _supportMessages.Setup(sm => sm.FindAllAsync(It.IsAny<Expression<Func<SupportMessage, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<SupportMessage>());

            var result = await _getAllTicketsHandler.Handle(new GetAllTicketsQuery(), It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.Items, Has.Count.EqualTo(1));
                Assert.That(result.Items[0].UnreadCount, Is.EqualTo(0));
                Assert.That(result.Items[0].ConversationStatus, Is.Null);
                Assert.That(result.Items[0].LastMessagePreview, Is.Null);
            });
        }

        [Test]
        public async Task EnsureGetSupportTicketByIdHandlerReturnsTicketWhenUserIsOwner()
        {
            var ticket = ValidTicket();
            _supportTickets.Setup(st => st.FindAsync(It.IsAny<Expression<Func<SupportTicket, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ticket);

            var result = await _getSupportTicketByIdHandler.Handle(new GetSupportTicketByIdQuery(ticket.TicketId), It.IsAny<CancellationToken>());

            Assert.Multiple(() =>
            {
                Assert.That(result.TicketId, Is.EqualTo(ticket.TicketId));
                Assert.That(result.Subject, Is.EqualTo("Order not delivered"));
            });
            _identityService.Verify(i => i.GetUserByIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Test]
        public async Task EnsureGetSupportTicketByIdHandlerReturnsTicketWhenUserIsSupportStaff()
        {
            var ticket = ValidTicket();
            ticket.CustomerId = Guid.NewGuid();
            _supportTickets.Setup(st => st.FindAsync(It.IsAny<Expression<Func<SupportTicket, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ticket);

            var user = new AppUser { Id = _userId, FName = "Support", LName = "Agent" };
            _identityService.Setup(i => i.GetUserByIdAsync(_userId)).ReturnsAsync(user);
            _roleService.Setup(r => r.GetAllRolesOfUser(user)).ReturnsAsync(new List<string> { "SupportAgent" });

            var result = await _getSupportTicketByIdHandler.Handle(new GetSupportTicketByIdQuery(ticket.TicketId), It.IsAny<CancellationToken>());

            Assert.That(result.TicketId, Is.EqualTo(ticket.TicketId));
        }

        [Test]
        public void EnsureGetSupportTicketByIdHandlerThrowsForbiddenWhenUserIsNotOwnerOrStaff()
        {
            var ticket = ValidTicket();
            ticket.CustomerId = Guid.NewGuid();
            _supportTickets.Setup(st => st.FindAsync(It.IsAny<Expression<Func<SupportTicket, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ticket);

            var user = new AppUser { Id = _userId, FName = "Another", LName = "Customer" };
            _identityService.Setup(i => i.GetUserByIdAsync(_userId)).ReturnsAsync(user);
            _roleService.Setup(r => r.GetAllRolesOfUser(user)).ReturnsAsync(new List<string> { "Customer" });

            Assert.ThrowsAsync<ForbiddenException>(async () =>
                await _getSupportTicketByIdHandler.Handle(new GetSupportTicketByIdQuery(ticket.TicketId), It.IsAny<CancellationToken>()));
        }

        [Test]
        public void EnsureGetSupportTicketByIdHandlerThrowsNotFoundWhenTicketNotFound()
        {
            _supportTickets.Setup(st => st.FindAsync(It.IsAny<Expression<Func<SupportTicket, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SupportTicket?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _getSupportTicketByIdHandler.Handle(new GetSupportTicketByIdQuery(Guid.NewGuid()), It.IsAny<CancellationToken>()));
        }
    }
}
