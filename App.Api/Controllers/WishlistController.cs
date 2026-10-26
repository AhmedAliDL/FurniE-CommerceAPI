using App.Application.Wishlists.Command.AddToWishlist;
using App.Application.Wishlists.Command.MoveWishlistItemToCart;
using App.Application.Wishlists.Command.RemoveFromWishlist;
using App.Application.Wishlists.Dto;
using App.Application.Wishlists.Queries.GetWishlist;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Api.Controllers
{
    /// <summary>
    /// Manages the authenticated user's product wishlist.
    /// </summary>
    [ApiController]
    [Route("api/wishlist")]
    [Authorize]
    public class WishlistController : BaseController
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WishlistController"/> class.
        /// </summary>
        /// <param name="sender">The MediatR sender used to dispatch commands and queries.</param>
        public WishlistController(ISender sender) : base(sender)
        {
        }
        /// <summary>
        /// Gets the current user's wishlist.
        /// </summary>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>The list of wishlist items.</returns>
        [ProducesResponseType(typeof(List<WishlistItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [HttpGet]
        public async Task<IActionResult> GetWishlist(CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(new GetWishlistQuery(), cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Adds a product to the current user's wishlist.
        /// </summary>
        /// <param name="productId">The product identifier.</param>
        /// <param name="productPrice">The product price.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>No content when the product is added or already present.</returns>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [HttpPost("{productId:guid}")]
        public async Task<IActionResult> AddToWishlist(Guid productId, decimal productPrice, CancellationToken cancellationToken = default)
        {
            await _sender.Send(new AddToWishlistCommand(productId, productPrice), cancellationToken);
            return NoContent();
        }

        /// <summary>
        /// Moves a product from the current user's wishlist into the shopping cart.
        /// </summary>
        /// <param name="productId">The product identifier.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A message when the item is moved, or the errors otherwise.</returns>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [HttpPost("{productId:guid}/move-to-cart")]
        public async Task<IActionResult> MoveToCart(Guid productId, CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(new MoveWishlistItemToCartCommand(productId), cancellationToken);
            if (result.IsSuccess)
                return Ok(new { message = result.Value });

            return BadRequest(new { success = false, errors = result.Errors });
        }

        /// <summary>
        /// Removes a product from the current user's wishlist.
        /// </summary>
        /// <param name="productId">The product identifier.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>No content when the product is removed.</returns>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [HttpDelete("{productId:guid}")]
        public async Task<IActionResult> RemoveFromWishlist(Guid productId, CancellationToken cancellationToken = default)
        {
            await _sender.Send(new RemoveFromWishlistCommand(productId), cancellationToken);
            return NoContent();
        }
    }
}
