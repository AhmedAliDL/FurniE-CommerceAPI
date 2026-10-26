using App.Application.Categories.Command.CreateCategory;
using App.Application.Categories.Command.DeleteCategory;
using App.Application.Categories.Command.UpdateCategory;
using App.Application.Categories.Dto;
using App.Application.Categories.Queries.GetAllCategories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Api.Controllers
{
    /// <summary>
    /// Provides product category listing and management endpoints.
    /// </summary>
    [ApiController]
    [Route("api/categories")]
    [Authorize(Roles = "Admin")]
    public class CategoryController : BaseController
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CategoryController"/> class.
        /// </summary>
        /// <param name="sender">The MediatR sender used to dispatch commands and queries.</param>
        public CategoryController(ISender sender) : base(sender)
        {
        }
        /// <summary>
        /// Gets all categories.
        /// </summary>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>The list of categories with their identifiers and names.</returns>
        [ProducesResponseType(typeof(List<CategoryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(new GetAllCategoriesQuery(), cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Creates a new category.
        /// </summary>
        /// <param name="command">The category name.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>The unique identifier of the created category.</returns>
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCategoryCommand command, CancellationToken cancellationToken = default)
        {
            var categoryId = await _sender.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetAll), new { id = categoryId }, categoryId);
        }

        /// <summary>
        /// Updates an existing category.
        /// </summary>
        /// <param name="categoryId">The category identifier.</param>
        /// <param name="command">The new category name.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>No content when the category is updated, not found when it does not exist, or conflict when the name is taken.</returns>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [HttpPut("{categoryId:guid}")]
        public async Task<IActionResult> Update(Guid categoryId, [FromBody] UpdateCategoryCommand command, CancellationToken cancellationToken = default)
        {
            if (categoryId != command.CategoryId)
                return BadRequest("The route category id must match the request category id.");

            var result = await _sender.Send(command, cancellationToken);
            return result ? NoContent() : NotFound();
        }

        /// <summary>
        /// Deletes a category.
        /// </summary>
        /// <param name="categoryId">The category identifier.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>No content when the category is deleted, not found when it does not exist, or conflict when it still contains products.</returns>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [HttpDelete("{categoryId:guid}")]
        public async Task<IActionResult> Delete(Guid categoryId, CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(new DeleteCategoryCommand(categoryId), cancellationToken);
            return result ? NoContent() : NotFound();
        }
    }
}
