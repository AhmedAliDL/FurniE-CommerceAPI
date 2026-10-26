using App.Application.Responses;
using App.Application.Reviews.Command.AddReview;
using App.Application.Reviews.Command.DeleteReview;
using App.Application.Reviews.Command.UpdateReview;
using App.Application.Reviews.Dto;
using App.Application.Reviews.Queries.GetProductReviews;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Api.Controllers
{
    /// <summary>
    /// Handles product reviews.
    /// </summary>
    [ApiController]
    [Route("api/reviews")]
    [Authorize]
    public class ReviewController : BaseController
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReviewController"/> class.
        /// </summary>
        /// <param name="sender">The MediatR sender used to dispatch commands and queries.</param>
        public ReviewController(ISender sender) : base(sender)
        {
        }
        /// <summary>
        /// Gets a page of reviews for a specific product.
        /// </summary>
        /// <param name="productId">The product identifier.</param>
        /// <param name="page">The page number, starting at 1.</param>
        /// <param name="pageSize">The number of reviews per page (1-100).</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A page of product reviews.</returns>
        [ProducesResponseType(typeof(PagedResult<ReviewDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [AllowAnonymous]
        [HttpGet("product/{productId:guid}")]
        public async Task<IActionResult> GetProductReviews(Guid productId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(new GetProductReviewsQuery(productId, page, pageSize), cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Adds a review for a specific product.
        /// </summary>
        /// <param name="command">The review details.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>The unique identifier of the created review.</returns>
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [HttpPost]
        public async Task<IActionResult> AddReview([FromBody] AddReviewCommand command, CancellationToken cancellationToken = default)
        {
            var reviewId = await _sender.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetProductReviews), new { productId = command.ProductId }, reviewId);
        }

        /// <summary>
        /// Updates a review. Customers can update their own reviews; admins can update any review.
        /// </summary>
        /// <param name="reviewId">The review identifier.</param>
        /// <param name="command">The new rating and comment.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>No content when updated, not found when it does not exist, or forbidden when not the owner.</returns>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [HttpPut("{reviewId:guid}")]
        public async Task<IActionResult> UpdateReview(Guid reviewId, [FromBody] UpdateReviewCommand command, CancellationToken cancellationToken = default)
        {
            if (reviewId != command.ReviewId)
                return BadRequest("The route review id must match the request review id.");

            var result = await _sender.Send(command, cancellationToken);
            return result ? NoContent() : NotFound();
        }

        /// <summary>
        /// Deletes a review. Customers can delete their own reviews; admins can delete any review.
        /// </summary>
        /// <param name="reviewId">The review identifier.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>No content when deleted, not found when it does not exist, or forbidden when not the owner.</returns>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [HttpDelete("{reviewId:guid}")]
        public async Task<IActionResult> DeleteReview(Guid reviewId, CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(new DeleteReviewCommand(reviewId), cancellationToken);
            return result ? NoContent() : NotFound();
        }
    }
}
