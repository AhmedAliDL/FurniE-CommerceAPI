using App.Application.Roles.Command.AssignRoleToUser;
using App.Application.Roles.Command.CreateRole;
using App.Application.Roles.Command.DeleteRole;
using App.Application.Roles.Command.UnassignRoleFromUser;
using App.Application.Roles.Dto;
using App.Application.Roles.Queries.GetRoles;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Api.Controllers
{
    /// <summary>
    /// Manages application roles and user role assignments. Admin only.
    /// </summary>
    [ApiController]
    [Route("api/roles")]
    [Authorize(Roles = "Admin")]
    public class RoleController : BaseController
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RoleController"/> class.
        /// </summary>
        /// <param name="sender">The MediatR sender used to dispatch commands and queries.</param>
        public RoleController(ISender sender) : base(sender)
        {
        }
        /// <summary>
        /// Gets all roles.
        /// </summary>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>The list of roles.</returns>
        [ProducesResponseType(typeof(List<RoleDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [HttpGet]
        public async Task<IActionResult> GetRoles(CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(new GetRolesQuery(), cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Creates a new role.
        /// </summary>
        /// <param name="command">The role details.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>Created status when the role is created.</returns>
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [HttpPost]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleCommand command, CancellationToken cancellationToken = default)
        {
            await _sender.Send(command, cancellationToken);
            return StatusCode(StatusCodes.Status201Created);
        }

        /// <summary>
        /// Assigns a role to a user.
        /// </summary>
        /// <param name="command">The role name and user email.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>No content when the role is assigned.</returns>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [HttpPost("assign")]
        public async Task<IActionResult> AssignRole([FromBody] AssignRoleToUserCommand command, CancellationToken cancellationToken = default)
        {
            await _sender.Send(command, cancellationToken);
            return NoContent();
        }

        /// <summary>
        /// Removes a role from a user.
        /// </summary>
        /// <param name="command">The role name and user email.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>No content when the role is removed.</returns>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [HttpPost("unassign")]
        public async Task<IActionResult> UnassignRole([FromBody] UnassignRoleFromUserCommand command, CancellationToken cancellationToken = default)
        {
            await _sender.Send(command, cancellationToken);
            return NoContent();
        }

        /// <summary>
        /// Deletes a role that is not built-in and has no assigned users.
        /// </summary>
        /// <param name="roleName">The role name.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>No content when deleted, not found when it does not exist, or conflict when it cannot be deleted.</returns>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [HttpDelete("{roleName}")]
        public async Task<IActionResult> DeleteRole(string roleName, CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(new DeleteRoleCommand(roleName), cancellationToken);
            return result ? NoContent() : NotFound();
        }
    }
}
