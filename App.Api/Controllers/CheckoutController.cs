using App.Application.Checkout.Command.PlaceOrder;
using App.Application.Checkout.Dto;
using App.Application.Checkout.Queries.GetCheckoutDetails;
using App.Application.ShoppingCarts.Command.UpdateCartQuantities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Api.Controllers
{
    /// <summary>
    /// Handles checkout details and order placement.
    /// </summary>
    [ApiController]
    [Route("api/checkout")]
    [Authorize]
    public class CheckoutController : BaseController
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CheckoutController"/> class.
        /// </summary>
        /// <param name="sender">The MediatR sender used to dispatch commands and queries.</param>
        public CheckoutController(ISender sender) : base(sender)
        {
        }
        /// <summary>
        /// Updates the cart quantities and gets the checkout details for the current user.
        /// </summary>
        /// <param name="command">The cart items with their new quantities.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>The checkout details.</returns>
        [ProducesResponseType(typeof(CheckoutDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [HttpPost]
        public async Task<IActionResult> Index([FromBody] UpdateCartQuantitiesCommand command, CancellationToken cancellationToken = default)
        {
            await _sender.Send(command, cancellationToken);
            CheckoutDto checkoutDetails = await _sender.Send(new GetCheckoutDetailsQuery(), cancellationToken);

            if (checkoutDetails.CartItems.Count > 0)
                return Ok(checkoutDetails);
            return BadRequest(new { message = "The shopping cart is empty." });
        }

        /// <summary>
        /// Places an order for the current user's shopping cart.
        /// </summary>
        /// <param name="command">The order placement details.</param>
        /// <param name="idempotencyKey">Optional key that makes retries safe: repeating the request with the same key returns the original order instead of creating a duplicate.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>The unique identifier of the created order.</returns>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [HttpPost("place-order")]
        public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderCommand command, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey = null, CancellationToken cancellationToken = default)
        {
            command.IdempotencyKey = idempotencyKey;
            var result = await _sender.Send(command, cancellationToken);

            if (result.IsSuccess)
                return Ok(new { message = "Order placed successfully.", orderId = result.Value });
            return BadRequest(new { success = false, errors = result.Errors });
        }

    }
}
