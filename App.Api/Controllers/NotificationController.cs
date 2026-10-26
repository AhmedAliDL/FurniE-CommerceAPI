using App.Application.Notifications.Command.MakeNotificationRead;
using App.Application.Notifications.Dto;
using App.Application.Notifications.Queries.GetUserNotifications;
using App.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Api.Controllers
{
    /// <summary>
    /// Handles admin notifications.
    /// </summary>
    [Route("api/notifications")]
    [Authorize(Roles = "Admin")]
    public class NotificationController : BaseController
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationController"/> class.
        /// </summary>
        /// <param name="sender">The MediatR sender used to dispatch commands and queries.</param>
        public NotificationController(ISender sender) : base(sender)
        {
        }

        /// <summary>
        /// Gets a page of the notifications of the currently authenticated admin.
        /// </summary>
        /// <param name="page">The page number, starting at 1.</param>
        /// <param name="pageSize">The number of notifications per page (1-100).</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A page of notifications.</returns>
        [ProducesResponseType(typeof(PagedResult<UserNotificationDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [HttpGet("admin")]
        public async Task<IActionResult> GetAdminNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(new GetUserNotificationsQuery(page, pageSize), cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Marks a notification as read.
        /// </summary>
        /// <param name="id">The notification identifier.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>An empty response on success, or an error message when the update fails.</returns>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [HttpPatch("{id:guid}/read")]
        public async Task<IActionResult> MakeNotificationRead(Guid id, CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(new MakeNotificationReadCommand(id), cancellationToken);
            if (result > 0)
                return Ok();
            return BadRequest("Can`t update notification right now try again later.");
        }

    }
}
