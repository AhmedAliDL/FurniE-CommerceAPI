using App.Application.Products.Command.DeleteProductImage;
using App.Application.Products.Command.ReorderProductImages;
using App.Application.Products.Command.SetPrimaryProductImage;
using App.Application.Products.Command.UploadProductImages;
using App.Application.Products.Dto;
using App.Application.Products.Queries.GetProductImages;
using App.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Api.Controllers
{
    /// <summary>
    /// Manages a product's images: listing, upload, primary selection, reordering, and deletion.
    /// </summary>
    [ApiController]
    [Route("api/products/{productId:guid}/images")]
    public class ProductImagesController : BaseController
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProductImagesController"/> class.
        /// </summary>
        /// <param name="sender">The MediatR sender used to dispatch commands and queries.</param>
        public ProductImagesController(ISender sender) : base(sender)
        {
        }
        /// <summary>
        /// Gets a product's images, primary first, then by display order.
        /// </summary>
        /// <param name="productId">The product identifier.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>The product images.</returns>
        [ProducesResponseType(typeof(ResponseResult<List<ProductImageDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetImages(Guid productId, CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(new GetProductImagesQuery(productId), cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Uploads one or more images for a product. The first uploaded image becomes primary when the product has none. Admin only.
        /// </summary>
        /// <param name="productId">The product identifier.</param>
        /// <param name="files">The image files (jpg, jpeg, png or webp).</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>The created image records.</returns>
        [ProducesResponseType(typeof(ResponseResult<List<ProductImageDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [Authorize(Roles = "Admin")]
        [Consumes("multipart/form-data")]
        [HttpPost]
        public async Task<IActionResult> Upload(Guid productId, [FromForm] IFormFileCollection? files, CancellationToken cancellationToken = default)
        {
            var command = new UploadProductImagesCommand
            {
                ProductId = productId,
                Files = files?.Select(file => new ImageFileUpload(file.FileName, file.ContentType, file.Length, file.OpenReadStream())).ToList() ?? []
            };

            var result = await _sender.Send(command, cancellationToken);
            return result.IsSuccess ? Ok(result) : BadRequest(result.Errors);
        }

        /// <summary>
        /// Marks an image as the product's primary image. Admin only.
        /// </summary>
        /// <param name="productId">The product identifier.</param>
        /// <param name="imageId">The image identifier.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>No content when the image is promoted.</returns>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [Authorize(Roles = "Admin")]
        [HttpPut("{imageId:guid}/primary")]
        public async Task<IActionResult> SetPrimary(Guid productId, Guid imageId, CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(new SetPrimaryProductImageCommand(productId, imageId), cancellationToken);
            return result.IsSuccess ? NoContent() : BadRequest(result.Errors);
        }

        /// <summary>
        /// Deletes (soft deletes) an image and removes the stored file. Admin only.
        /// </summary>
        /// <param name="productId">The product identifier.</param>
        /// <param name="imageId">The image identifier.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>No content when the image is deleted.</returns>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [Authorize(Roles = "Admin")]
        [HttpDelete("{imageId:guid}")]
        public async Task<IActionResult> Delete(Guid productId, Guid imageId, CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(new DeleteProductImageCommand(productId, imageId), cancellationToken);
            return result.IsSuccess ? NoContent() : BadRequest(result.Errors);
        }

        /// <summary>
        /// Reorders a product's images. The list must contain every image id exactly once. Admin only.
        /// </summary>
        /// <param name="productId">The product identifier.</param>
        /// <param name="command">The ordered image ids.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>No content when the order is updated.</returns>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [Authorize(Roles = "Admin")]
        [HttpPut("order")]
        public async Task<IActionResult> Reorder(Guid productId, [FromBody] ReorderProductImagesCommand command, CancellationToken cancellationToken = default)
        {
            if (productId != command.ProductId)
                return BadRequest("The route product id must match the request product id.");

            var result = await _sender.Send(command, cancellationToken);
            return result.IsSuccess ? NoContent() : BadRequest(result.Errors);
        }
    }
}
