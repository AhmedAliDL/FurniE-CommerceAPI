using App.Application.Responses;
using App.Application.SupportTickets.Command.CreateSupportTicket;
using App.Application.SupportTickets.Command.UpdateTicketStatus;
using App.Application.SupportTickets.Dto;
using App.Application.SupportTickets.Queries.GetAllTickets;
using App.Application.SupportTickets.Queries.GetMySupportTickets;
using App.Application.SupportTickets.Queries.GetSupportTicketById;
using App.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Api.Controllers
{
    /// <summary>
    /// Handles support tickets for customers and support staff.
    /// </summary>
    [ApiController]
    [Route("api/support-tickets")]
    [Authorize]
    public class SupportTicketsController : BaseController
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SupportTicketsController"/> class.
        /// </summary>
        /// <param name="sender">The MediatR sender used to dispatch commands and queries.</param>
        public SupportTicketsController(ISender sender) : base(sender)
        {
        }
        /// <summary>
        /// Gets a page of the authenticated customer's support tickets.
        /// </summary>
        /// <param name="page">The page number, starting at 1.</param>
        /// <param name="pageSize">The number of tickets per page (1-100).</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A page of the customer's support tickets.</returns>
        [ProducesResponseType(typeof(PagedResult<SupportTicketDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [HttpGet]
        public async Task<IActionResult> GetMyTickets([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        {
            var tickets = await _sender.Send(new GetMySupportTicketsQuery(page, pageSize), cancellationToken);
            return Ok(tickets);
        }

        /// <summary>
        /// Gets a page of all support tickets with their conversation state. Admin or SupportAgent only.
        /// </summary>
        /// <param name="status">Optional ticket status filter.</param>
        /// <param name="page">The page number, starting at 1.</param>
        /// <param name="pageSize">The number of tickets per page (1-100).</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A page of support tickets for the agents' inbox.</returns>
        [ProducesResponseType(typeof(PagedResult<AdminSupportTicketDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [Authorize(Roles = "Admin,SupportAgent")]
        [HttpGet("all")]
        public async Task<IActionResult> GetAll([FromQuery] SupportTicketStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        {
            var tickets = await _sender.Send(new GetAllTicketsQuery(status, page, pageSize), cancellationToken);
            return Ok(tickets);
        }

        /// <summary>
        /// Gets a support ticket by its unique identifier. Only the owner or an admin can view it.
        /// </summary>
        /// <param name="ticketId">The ticket identifier.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>The support ticket.</returns>
        [ProducesResponseType(typeof(SupportTicketDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [HttpGet("{ticketId:guid}")]
        public async Task<IActionResult> GetById(Guid ticketId, CancellationToken cancellationToken = default)
        {
            var ticket = await _sender.Send(new GetSupportTicketByIdQuery(ticketId), cancellationToken);
            return Ok(ticket);
        }

        /// <summary>
        /// Creates a new support ticket for the authenticated customer.
        /// </summary>
        /// <param name="command">The ticket details.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>The unique identifier of the created ticket.</returns>
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSupportTicketCommand command, CancellationToken cancellationToken = default)
        {
            var ticketId = await _sender.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { ticketId }, ticketId);
        }

        /// <summary>
        /// Updates the status of a support ticket. Admin or SupportAgent only.
        /// </summary>
        /// <param name="ticketId">The ticket identifier.</param>
        /// <param name="command">The new status.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>No content when the status is updated.</returns>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "Admin,SupportAgent")]
        [HttpPut("{ticketId:guid}/status")]
        public async Task<IActionResult> UpdateStatus(Guid ticketId, [FromBody] UpdateTicketStatusCommand command, CancellationToken cancellationToken = default)
        {
            if (ticketId != command.TicketId)
                return BadRequest("The route ticket id must match the request ticket id.");

            await _sender.Send(command, cancellationToken);
            return NoContent();
        }
    }
}
