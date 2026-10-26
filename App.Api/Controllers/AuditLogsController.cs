using App.Application.AuditLogs.Dto;
using App.Application.AuditLogs.Queries.GetAuditLogs;
using App.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Api.Controllers
{
    /// <summary>
    /// Provides read access to the audit trail. Admin only.
    /// </summary>
    [ApiController]
    [Route("api/audit-logs")]
    [Authorize(Roles = "Admin")]
    public class AuditLogsController : BaseController
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AuditLogsController"/> class.
        /// </summary>
        /// <param name="sender">The MediatR sender used to dispatch commands and queries.</param>
        public AuditLogsController(ISender sender) : base(sender)
        {
        }

        /// <summary>
        /// Gets a page of audit log entries, newest first.
        /// </summary>
        /// <param name="query">The pagination and entity-name filters.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A page of audit log entries.</returns>
        [ProducesResponseType(typeof(PagedResult<AuditLogDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [HttpGet]
        public async Task<IActionResult> GetAuditLogs([FromQuery] GetAuditLogsQuery query, CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(query, cancellationToken);
            return Ok(result);
        }
    }
}
