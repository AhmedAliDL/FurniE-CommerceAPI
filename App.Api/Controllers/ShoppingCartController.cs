using App.Application.ShoppingCarts.Command.AddCartItem;
using App.Application.ShoppingCarts.Command.ClearCart;
using App.Application.ShoppingCarts.Command.DeleteCartItem;
using App.Application.ShoppingCarts.Command.UpdateCartQuantities;
using App.Application.ShoppingCarts.Dto;
using App.Application.ShoppingCarts.Queries.GetCartItems;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Api.Controllers
{
    /// <summary>
    /// Manages the authenticated user's shopping cart.
    /// </summary>
    [ApiController]
    [Route("api/cart")]
    [Authorize]
    public class ShoppingCartController : BaseController
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ShoppingCartController"/> class.
        /// </summary>
        /// <param name="sender">The MediatR sender used to dispatch commands and queries.</param>
        public ShoppingCartController(ISender sender) : base(sender)
        {
        }
        /// <summary>
        /// Gets the current user's shopping cart items.
        /// </summary>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>The shopping cart with its items.</returns>
        [ProducesResponseType(typeof(ShoppingCartDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(new GetCartItemsQuery(), cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Removes all items from the current user's shopping cart.
        /// </summary>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>No content when the cart is cleared.</returns>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [HttpDelete]
        public async Task<IActionResult> ClearCart(CancellationToken cancellationToken = default)
        {
            await _sender.Send(new ClearCartCommand(), cancellationToken);
            return NoContent();
        }

        /// <summary>
        /// Adds a product to the current user's shopping cart.
        /// </summary>
        /// <param name="productId">The product identifier.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A message when the item is added, or the errors otherwise.</returns>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpPost("items/{productId:guid}")]
        public async Task<IActionResult> AddItem(Guid productId, CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(new AddCartItemCommand(productId), cancellationToken);
            if (result.IsSuccess)
                return Ok(new { message = result.Value });

            return BadRequest(new { success = false, errors = result.Errors });
        }

        /// <summary>
        /// Updates the quantity of products in the shopping cart.
        /// </summary>
        /// <param name="command">The cart items with their new quantities.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>No content when the quantities are updated.</returns>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [HttpPut("quantities")]
        public async Task<IActionResult> UpdateQuantity([FromBody] UpdateCartQuantitiesCommand command, CancellationToken cancellationToken = default)
        {
            await _sender.Send(command, cancellationToken);
            return NoContent();
        }

        /// <summary>
        /// Deletes a product from the shopping cart.
        /// </summary>
        /// <param name="productId">The product identifier.</param>
        /// <param name="shoppingCartId">The shopping cart identifier.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>No content when the item is deleted.</returns>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [HttpDelete("items/{productId:guid}")]
        public async Task<IActionResult> DeleteProduct(Guid productId, [FromQuery] Guid shoppingCartId, CancellationToken cancellationToken = default)
        {
            await _sender.Send(new DeleteCartItemCommand(productId, shoppingCartId), cancellationToken);
            return NoContent();
        }
    }
}
