using System.Text.Json;
using App.Application.SupportTickets.Command.CreateSupportTicket;
using App.Application.SupportTickets.Command.UpdateTicketStatus;
using App.Application.SupportTickets.Dto;
using App.Application.SupportTickets.Queries.GetAllTickets;
using App.Application.SupportTickets.Queries.GetMySupportTickets;
using App.Application.SupportTickets.Queries.GetSupportTicketById;
using App.Application.Responses;
using App.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using App.Api.Controllers;

namespace App.Test.Controllers
{
    [TestFixture]
    [Category("SupportTickets")]
    public class SupportTicketsControllerTest
    {
        private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);
        private Mock<IMediator> _sender = null!;
        private SupportTicketsController _controller = null!;

        [SetUp]
        public void Setup()
        {
            _sender = new Mock<IMediator>();
            _controller = new SupportTicketsController(_sender.Object);
        }

        [Test]
        public void EnsureThatCreateSupportTicketCommandBindsPriorityFromString()
        {
            var command = JsonSerializer.Deserialize<CreateSupportTicketCommand>(
                "{\"subject\":\"Order not delivered\",\"priority\":\"High\"}", WebJson);

            Assert.That(command!.Priority, Is.EqualTo(SupportTicketPriority.High));
        }

        [Test]
        public void EnsureThatCreateSupportTicketCommandStillBindsPriorityFromNumber()
        {
            var command = JsonSerializer.Deserialize<CreateSupportTicketCommand>(
                "{\"subject\":\"Order not delivered\",\"priority\":3}", WebJson);

            Assert.That(command!.Priority, Is.EqualTo(SupportTicketPriority.High));
        }

        [Test]
        public void EnsureThatUpdateTicketStatusCommandBindsStatusFromString()
        {
            var ticketId = Guid.NewGuid();

            var command = JsonSerializer.Deserialize<UpdateTicketStatusCommand>(
                $"{{\"ticketId\":\"{ticketId}\",\"status\":\"InProgress\"}}", WebJson);

            Assert.Multiple(() =>
            {
                Assert.That(command!.TicketId, Is.EqualTo(ticketId));
                Assert.That(command.Status, Is.EqualTo(SupportTicketStatus.InProgress));
            });
        }

        [Test]
        public async Task EnsureThatGetMyTicketsReturnsOkWhenTicketsExist()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<GetMySupportTicketsQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedResult<SupportTicketDto> { Page = 1, PageSize = 20, TotalCount = 0 });

            var result = await _controller.GetMyTickets();

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatGetAllReturnsOkWhenNoStatusFilter()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<GetAllTicketsQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedResult<AdminSupportTicketDto> { Page = 1, PageSize = 20, TotalCount = 0 });

            var result = await _controller.GetAll(null);

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatGetAllReturnsOkWhenFilteredByStatus()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<GetAllTicketsQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedResult<AdminSupportTicketDto> { Page = 1, PageSize = 20, TotalCount = 0 });

            var result = await _controller.GetAll(SupportTicketStatus.Open);

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatGetByIdReturnsOkWhenTicketExists()
        {
            _sender.Setup(s => s.Send(
                    It.IsAny<GetSupportTicketByIdQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SupportTicketDto());

            var result = await _controller.GetById(Guid.NewGuid());

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task EnsureThatCreateReturnsCreatedWhenTicketIsValid()
        {
            var command = new CreateSupportTicketCommand("Order not delivered", SupportTicketPriority.High);
            _sender.Setup(s => s.Send(
                    It.IsAny<CreateSupportTicketCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Guid.NewGuid());

            var result = await _controller.Create(command);

            Assert.That(result, Is.TypeOf<CreatedAtActionResult>());
        }

        [Test]
        public async Task EnsureThatUpdateStatusReturnsNoContentWhenStatusUpdated()
        {
            var ticketId = Guid.NewGuid();
            var command = new UpdateTicketStatusCommand(ticketId, SupportTicketStatus.InProgress);
            _sender.Setup(s => s.Send(
                    It.IsAny<UpdateTicketStatusCommand>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.UpdateStatus(ticketId, command);

            Assert.That(result, Is.TypeOf<NoContentResult>());
        }

        [Test]
        public async Task EnsureThatUpdateStatusReturnsBadRequestWhenRouteIdMismatch()
        {
            var command = new UpdateTicketStatusCommand(Guid.NewGuid(), SupportTicketStatus.InProgress);

            var result = await _controller.UpdateStatus(Guid.NewGuid(), command);

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
            _sender.Verify(s => s.Send(It.IsAny<UpdateTicketStatusCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
