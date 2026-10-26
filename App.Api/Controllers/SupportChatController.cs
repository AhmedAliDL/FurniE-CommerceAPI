using App.Application.Responses;
using App.Application.SupportChat.Command.CloseConversation;
using App.Application.SupportChat.Command.MarkConversationRead;
using App.Application.SupportChat.Command.ReopenConversation;
using App.Application.SupportChat.Dto;
using App.Application.SupportChat.Queries.GetConversation;
using App.Application.SupportChat.Queries.GetConversationMessages;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Api.Controllers
{
    /// <summary>
    /// Handles the chat conversation attached to a support ticket. Sending messages is done through the SupportHub SignalR connection only.
    /// </summary>
    [ApiController]
    [Route("api/support-tickets/{ticketId:guid}/chat")]
    [Authorize]
    public class SupportChatController : BaseController
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SupportChatController"/> class.
        /// </summary>
        /// <param name="sender">The MediatR sender used to dispatch commands and queries.</param>
        public SupportChatController(ISender sender) : base(sender)
        {
        }
        /// <summary>
        /// Gets the conversation attached to the ticket.
        /// </summary>
        /// <param name="ticketId">The ticket identifier.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>The conversation, or not found when no message has been sent yet.</returns>
        [ProducesResponseType(typeof(SupportConversationDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [HttpGet]
        public async Task<IActionResult> GetConversation(Guid ticketId, CancellationToken cancellationToken = default)
        {
            var conversation = await _sender.Send(new GetConversationQuery(ticketId), cancellationToken);
            return Ok(conversation);
        }

        /// <summary>
        /// Gets a page of the message history of the conversation, oldest first.
        /// </summary>
        /// <param name="ticketId">The ticket identifier.</param>
        /// <param name="page">The page number, starting at 1.</param>
        /// <param name="pageSize">The number of messages per page (1-100).</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A page of the messages of the conversation.</returns>
        [ProducesResponseType(typeof(PagedResult<SupportMessageDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [HttpGet("messages")]
        public async Task<IActionResult> GetMessages(Guid ticketId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
        {
            var messages = await _sender.Send(new GetConversationMessagesQuery(ticketId, page, pageSize), cancellationToken);
            return Ok(messages);
        }

        /// <summary>
        /// Marks all messages sent by the other party as read.
        /// </summary>
        /// <param name="ticketId">The ticket identifier.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>The number of marked messages.</returns>
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [HttpPost("read")]
        public async Task<IActionResult> MarkRead(Guid ticketId, CancellationToken cancellationToken = default)
        {
            var markedCount = await _sender.Send(new MarkConversationReadCommand(ticketId), cancellationToken);
            return Ok(new { marked = markedCount });
        }

        /// <summary>
        /// Closes the conversation. Admin or SupportAgent only.
        /// </summary>
        /// <param name="ticketId">The ticket identifier.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>No content when the conversation is closed.</returns>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "Admin,SupportAgent")]
        [HttpPost("close")]
        public async Task<IActionResult> Close(Guid ticketId, CancellationToken cancellationToken = default)
        {
            await _sender.Send(new CloseConversationCommand(ticketId), cancellationToken);
            return NoContent();
        }

        /// <summary>
        /// Reopens a closed conversation. Admin or SupportAgent only.
        /// </summary>
        /// <param name="ticketId">The ticket identifier.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>No content when the conversation is reopened.</returns>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "Admin,SupportAgent")]
        [HttpPost("reopen")]
        public async Task<IActionResult> Reopen(Guid ticketId, CancellationToken cancellationToken = default)
        {
            await _sender.Send(new ReopenConversationCommand(ticketId), cancellationToken);
            return NoContent();
        }
    }
}
