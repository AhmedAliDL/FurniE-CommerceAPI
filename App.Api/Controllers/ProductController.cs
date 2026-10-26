using App.Application.Categories.Queries.GetAllCategories;
using App.Application.Products.Command.CreateProduct;
using App.Application.Products.Command.DeleteProduct;
using App.Application.Products.Command.UpdateProduct;
using App.Application.Products.Dto;
using App.Application.Products.Queries.GetActiveProducts;
using App.Application.Products.Queries.GetAllProducts;
using App.Application.Products.Queries.GetProductById;
using App.Application.Products.Queries.GetProductDetails;
using App.Application.Products.Queries.GetProductsByCategory;
using App.Application.Products.Queries.GetRecentlyViewed;
using App.Application.Products.Queries.SearchProducts;
using App.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Api.Controllers
{
    /// <summary>
    /// Provides product catalog and product management endpoints.
    /// </summary>
    [ApiController]
    [Route("api/products")]
    [Authorize]
    public class ProductController : BaseController
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProductController"/> class.
        /// </summary>
        /// <param name="sender">The MediatR sender used to dispatch commands and queries.</param>
        public ProductController(ISender sender) : base(sender)
        {
        }
        /// <summary>
        /// Gets a page of all products.
        /// </summary>
        /// <param name="page">The page number, starting at 1.</param>
        /// <param name="pageSize">The number of products per page (1-100).</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A page of all products.</returns>
        [ProducesResponseType(typeof(PagedResult<ProductResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(new GetAllProductsQuery(page, pageSize), cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Searches products with optional filtering, sorting and pagination.
        /// </summary>
        /// <param name="query">The search criteria.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A page of matching products.</returns>
        [ProducesResponseType(typeof(PagedResult<ProductListItemDto>), StatusCodes.Status200OK)]
        [AllowAnonymous]
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] SearchProductsQuery query, CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Gets a page of the active products with the category list for browsing.
        /// </summary>
        /// <param name="page">The page number, starting at 1.</param>
        /// <param name="pageSize">The number of products per page (1-100).</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A page of the active products and all categories.</returns>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [AllowAnonymous]
        [HttpGet("active")]
        public async Task<IActionResult> GetActive([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        {
            var products = await _sender.Send(new GetActiveProductsQuery(page, pageSize), cancellationToken);
            var categories = await _sender.Send(new GetAllCategoriesQuery(), cancellationToken);
            return Ok(new { products, categories });
        }

        /// <summary>
        /// Gets a page of the active products in a category.
        /// </summary>
        /// <param name="categoryName">The category name.</param>
        /// <param name="page">The page number, starting at 1.</param>
        /// <param name="pageSize">The number of products per page (1-100).</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A page of the active products in the category and all categories.</returns>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [AllowAnonymous]
        [HttpGet("category/{categoryName}")]
        public async Task<IActionResult> GetByCategory(string categoryName, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        {
            var products = await _sender.Send(new GetProductsByCategoryQuery(categoryName, page, pageSize), cancellationToken);
            var categories = await _sender.Send(new GetAllCategoriesQuery(), cancellationToken);
            return Ok(new { products, categories });
        }

        /// <summary>
        /// Gets a product's details with its reviews.
        /// </summary>
        /// <param name="productId">The product identifier.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>The product details with reviews.</returns>
        [ProducesResponseType(typeof(ProductDetailsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [AllowAnonymous]
        [HttpGet("{productId:guid}/details")]
        public async Task<IActionResult> GetDetails(Guid productId, CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(new GetProductDetailsQuery(productId), cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Gets a product by its unique identifier.
        /// </summary>
        /// <param name="productId">The product identifier.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>The product details.</returns>
        [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [AllowAnonymous]
        [HttpGet("{productId:guid}")]
        public async Task<IActionResult> GetById(Guid productId, CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(new GetProductByIdQuery(productId), cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Gets the products recently viewed by the current customer.
        /// </summary>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>The recently viewed products, most recent first.</returns>
        [ProducesResponseType(typeof(List<ProductResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize]
        [HttpGet("recently-viewed")]
        public async Task<IActionResult> GetRecentlyViewed(CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(new GetRecentlyViewedQuery(), cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Creates a new product.
        /// </summary>
        /// <param name="command">The product details.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>The unique identifier of the created product.</returns>
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProductCommand command, CancellationToken cancellationToken = default)
        {
            var productId = await _sender.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { productId }, productId);
        }

        /// <summary>
        /// Updates an existing product.
        /// </summary>
        /// <param name="productId">The product identifier.</param>
        /// <param name="command">The product details.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>No content when the product is updated, or not found when it does not exist.</returns>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [Authorize(Roles = "Admin")]
        [HttpPut("{productId:guid}")]
        public async Task<IActionResult> Update(Guid productId, [FromBody] UpdateProductCommand command, CancellationToken cancellationToken = default)
        {
            if (productId != command.ProductId)
                return BadRequest("The route product id must match the request product id.");

            var result = await _sender.Send(command, cancellationToken);
            return result ? NoContent() : NotFound();
        }

        /// <summary>
        /// Deletes a product that has no order history.
        /// </summary>
        /// <param name="productId">The product identifier.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>No content when the product is deleted, not found when it does not exist, or conflict when it has been ordered.</returns>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [Authorize(Roles = "Admin")]
        [HttpDelete("{productId:guid}")]
        public async Task<IActionResult> Delete(Guid productId, CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(new DeleteProductCommand(productId), cancellationToken);
            return result ? NoContent() : NotFound();
        }
    }
}
