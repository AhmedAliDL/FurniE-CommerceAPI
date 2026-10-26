using App.Application.Coupons.Command.CreateCoupon;
using App.Application.Coupons.Command.DeleteCoupon;
using App.Application.Coupons.Command.UpdateCoupon;
using App.Application.Coupons.Dto;
using App.Application.Coupons.Queries.CheckCoupon;
using App.Application.Coupons.Queries.GetAllCoupons;
using App.Application.Coupons.Queries.GetCouponForEdit;
using App.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Api.Controllers
{
    /// <summary>
    /// Provides coupon listing, validation, and management endpoints.
    /// </summary>
    [ApiController]
    [Route("api/coupons")]
    [Authorize]
    public class CouponController : BaseController
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CouponController"/> class.
        /// </summary>
        /// <param name="sender">The MediatR sender used to dispatch commands and queries.</param>
        public CouponController(ISender sender) : base(sender)
        {
        }
        /// <summary>
        /// Gets a page of all coupons.
        /// </summary>
        /// <param name="page">The page number, starting at 1.</param>
        /// <param name="pageSize">The number of coupons per page (1-100).</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A page of all coupons.</returns>
        [ProducesResponseType(typeof(PagedResult<CouponDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAllCoupons([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(new GetAllCouponsQuery(page, pageSize), cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Checks if a coupon code is valid.
        /// </summary>
        /// <param name="couponCode">The coupon code.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>The discount percentage when the coupon is valid.</returns>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [HttpGet("check/{couponCode}")]
        public async Task<IActionResult> CheckCoupon(string couponCode, CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(new CheckCouponQuery(couponCode), cancellationToken);

            if (result.IsSuccess)
                return Ok(new { success = true, data = new { discountPercentage = result.Value } });
            return Ok(new { success = false, message = result.Errors.FirstOrDefault() });
        }

        /// <summary>
        /// Gets a coupon for editing.
        /// </summary>
        /// <param name="couponId">The coupon identifier.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>The coupon details.</returns>
        [ProducesResponseType(typeof(EditCouponDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [Authorize(Roles = "Admin")]
        [HttpGet("{couponId:guid}")]
        public async Task<IActionResult> GetForEdit(Guid couponId, CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(new GetCouponForEditQuery(couponId), cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Creates a new coupon.
        /// </summary>
        /// <param name="command">The coupon details.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>The unique identifier of the created coupon.</returns>
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> AddCoupon([FromBody] CreateCouponCommand command, CancellationToken cancellationToken = default)
        {
            var couponId = await _sender.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetForEdit), new { couponId }, couponId);
        }

        /// <summary>
        /// Updates an existing coupon.
        /// </summary>
        /// <param name="couponId">The coupon identifier.</param>
        /// <param name="command">The coupon details.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>No content when the coupon is updated, or not found when it does not exist.</returns>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [Authorize(Roles = "Admin")]
        [HttpPut("{couponId:guid}")]
        public async Task<IActionResult> EditCoupon(Guid couponId, [FromBody] UpdateCouponCommand command, CancellationToken cancellationToken = default)
        {
            if (couponId != command.CouponId)
                return BadRequest("The route coupon id must match the request coupon id.");

            var result = await _sender.Send(command, cancellationToken);
            return result ? NoContent() : NotFound();
        }

        /// <summary>
        /// Deletes a coupon.
        /// </summary>
        /// <param name="couponId">The coupon identifier.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>No content when the coupon is deleted, or not found when it does not exist.</returns>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [Authorize(Roles = "Admin")]
        [HttpDelete("{couponId:guid}")]
        public async Task<IActionResult> DeleteCoupon(Guid couponId, CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(new DeleteCouponCommand(couponId), cancellationToken);
            return result ? NoContent() : NotFound();
        }
    }
}
