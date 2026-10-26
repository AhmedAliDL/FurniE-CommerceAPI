using App.Application.Contact.Command.SendContactEmail;
using App.Application.Contact.Dto;
using App.Application.Contact.Queries.GetContactDetails;
using App.Application.Notifications.Dto;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Api.Controllers
{
    /// <summary>
    /// Handles contact details and contact-form email delivery.
    /// </summary>
    [ApiController]
    [Route("api/contact")]
    [Authorize]
    public class ContactController : BaseController
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContactController"/> class.
        /// </summary>
        /// <param name="sender">The MediatR sender used to dispatch commands and queries.</param>
        public ContactController(ISender sender) : base(sender)
        {
        }
        /// <summary>
        /// Gets the current user's contact details.
        /// </summary>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>The contact details.</returns>
        [ProducesResponseType(typeof(ContactDetailsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpGet]
        public async Task<IActionResult> GetContactDetails(CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(new GetContactDetailsQuery(), cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Sends a contact email from the current user to the site admin.
        /// </summary>
        /// <param name="command">The email subject and body.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>The result of the email sending operation.</returns>
        [ProducesResponseType(typeof(NotifyDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [HttpPost("send-email")]
        public async Task<IActionResult> SendEmail([FromBody] SendContactEmailCommand command, CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(command, cancellationToken);
            return Ok(result);
        }
    }
}
