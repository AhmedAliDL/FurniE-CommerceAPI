using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace App.Api.Controllers
{
    /// <summary>
    /// Base class for API controllers that dispatch commands and queries through MediatR.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class BaseController : ControllerBase
    {
        /// <summary>
        /// The MediatR sender used to dispatch commands and queries.
        /// </summary>
        protected readonly ISender _sender;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseController"/> class.
        /// </summary>
        /// <param name="sender">The MediatR sender used to dispatch commands and queries.</param>
        public BaseController(ISender sender)
        {
            _sender = sender;
        }
    }
}
