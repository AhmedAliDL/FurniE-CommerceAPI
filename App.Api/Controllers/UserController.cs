using App.Application.Responses;
using App.Application.Users.Command.UpdateUserStatus;
using App.Application.Users.Dto;
using App.Application.Users.Queries.GetUsers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Api.Controllers
{
    /// <summary>
    /// Provides user administration endpoints. Admin only.
    /// </summary>
    [ApiController]
    [Route("api/users")]
    [Authorize(Roles = "Admin")]
    public class UserController : BaseController
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserController"/> class.
        /// </summary>
        /// <param name="sender">The MediatR sender used to dispatch commands and queries.</param>
        public UserController(ISender sender) : base(sender)
        {
        }

        /// <summary>
        /// Gets a page of users with their roles.
        /// </summary>
        /// <param name="query">The pagination and search criteria.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A page of users.</returns>
        [ProducesResponseType(typeof(PagedResult<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] GetUsersQuery query, CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Activates or deactivates a user account.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="command">The new active status.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>No content when updated, not found when the user does not exist, or conflict when deactivating your own account.</returns>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [HttpPut("{userId:guid}/status")]
        public async Task<IActionResult> UpdateStatus(Guid userId, [FromBody] UpdateUserStatusCommand command, CancellationToken cancellationToken = default)
        {
            if (userId != command.UserId)
                return BadRequest("The route user id must match the request user id.");

            var result = await _sender.Send(command, cancellationToken);
            return result ? NoContent() : NotFound();
        }
    }
}
