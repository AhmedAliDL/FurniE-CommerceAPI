using App.Application.Orders.Command.AcceptReturnRequest;
using App.Application.Orders.Command.ApproveCancellationRequest;
using App.Application.Orders.Command.CancelOrder;
using App.Application.Orders.Command.DeleteOrder;
using App.Application.Orders.Command.DeleteOrderItem;
using App.Application.Orders.Command.ProcessReturnRefund;
using App.Application.Orders.Command.ReceiveReturnRequest;
using App.Application.Orders.Command.RejectCancellationRequest;
using App.Application.Orders.Command.RejectReturnRequest;
using App.Application.Orders.Command.RequestOrderCancellation;
using App.Application.Orders.Command.RequestOrderReturn;
using App.Application.Orders.Command.UpdateOrderStatus;
using App.Application.Orders.Dto;
using App.Application.Orders.Queries.GetCancellationRequests;
using App.Application.Orders.Queries.GetOrderDetails;
using App.Application.Orders.Queries.GetReturnRequests;
using App.Application.Orders.Queries.GetUserOrders;
using App.Application.Responses;
using App.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Api.Controllers
{
    /// <summary>
    /// Handles order history, order details, item removal, status updates, and cancellation and return workflows.
    /// </summary>
    [ApiController]
    [Route("api/orders")]
    [Authorize]
    public class OrdersController : BaseController
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OrdersController"/> class.
        /// </summary>
        /// <param name="sender">The MediatR sender used to dispatch commands and queries.</param>
        public OrdersController(ISender sender) : base(sender)
        {
        }
        /// <summary>
        /// Gets a page of the current user's order list.
        /// </summary>
        /// <param name="page">The page number, starting at 1.</param>
        /// <param name="pageSize">The number of orders per page (1-100).</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A page of user orders.</returns>
        [ProducesResponseType(typeof(PagedResult<OrderShippingDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [HttpGet]
        public async Task<IActionResult> GetOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(new GetUserOrdersQuery(page, pageSize), cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Gets the details of a specific order.
        /// </summary>
        /// <param name="orderId">The order identifier.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>The order details.</returns>
        [ProducesResponseType(typeof(List<OrderDetailsDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [HttpGet("{orderId:guid}/details")]
        public async Task<IActionResult> OrderDetails(Guid orderId, CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(new GetOrderDetailsQuery(orderId), cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Updates the status of an order following the allowed state transitions. Admin only.
        /// </summary>
        /// <param name="orderId">The order identifier.</param>
        /// <param name="command">The new order status.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>No content when the status is updated.</returns>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "Admin")]
        [HttpPut("{orderId:guid}/status")]
        public async Task<IActionResult> UpdateOrderStatus(Guid orderId, [FromBody] UpdateOrderStatusCommand command, CancellationToken cancellationToken = default)
        {
            if (orderId != command.OrderId)
                return BadRequest("The route order id must match the request order id.");

            await _sender.Send(command, cancellationToken);
            return NoContent();
        }

        /// <summary>
        /// Cancels the current user's order. Only allowed while the order is pending, confirmed or processing.
        /// </summary>
        /// <param name="orderId">The order identifier.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>No content when the order is cancelled.</returns>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [HttpPut("{orderId:guid}/cancel")]
        public async Task<IActionResult> CancelOrder(Guid orderId, CancellationToken cancellationToken = default)
        {
            await _sender.Send(new CancelOrderCommand(orderId), cancellationToken);
            return NoContent();
        }

        /// <summary>
        /// Requests cancellation of a paid order before it ships. The order moves to CancellationRequested and the admin is notified.
        /// </summary>
        /// <param name="orderId">The order identifier.</param>
        /// <param name="command">The cancellation reason.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>The id of the created cancellation request.</returns>
        [ProducesResponseType(typeof(ResponseResult<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [HttpPost("{orderId:guid}/cancellation-request")]
        public async Task<IActionResult> RequestOrderCancellation(Guid orderId, [FromBody] RequestOrderCancellationCommand command, CancellationToken cancellationToken = default)
        {
            if (orderId != command.OrderId)
                return BadRequest("The route order id must match the request order id.");

            var result = await _sender.Send(command, cancellationToken);
            return result.IsSuccess ? Ok(result) : BadRequest(result.Errors);
        }

        /// <summary>
        /// Requests a return for a delivered order (within 14 days of delivery).
        /// </summary>
        /// <param name="orderId">The order identifier.</param>
        /// <param name="command">The return reason category, free-text reason and the items to return.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>The id of the created return request.</returns>
        [ProducesResponseType(typeof(ResponseResult<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [HttpPost("{orderId:guid}/return-request")]
        public async Task<IActionResult> RequestOrderReturn(Guid orderId, [FromBody] RequestOrderReturnCommand command, CancellationToken cancellationToken = default)
        {
            if (orderId != command.OrderId)
                return BadRequest("The route order id must match the request order id.");

            var result = await _sender.Send(command, cancellationToken);
            return result.IsSuccess ? Ok(result) : BadRequest(result.Errors);
        }

        /// <summary>
        /// Lists a page of cancellation requests, optionally filtered by status. Admin only.
        /// </summary>
        /// <param name="status">Optional status filter (Pending, Approved, Rejected).</param>
        /// <param name="page">The page number, starting at 1.</param>
        /// <param name="pageSize">The number of requests per page (1-100).</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A page of cancellation requests.</returns>
        [ProducesResponseType(typeof(ResponseResult<PagedResult<CancellationRequestListDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "Admin")]
        [HttpGet("cancellation-requests")]
        public async Task<IActionResult> GetCancellationRequests([FromQuery] CancellationRequestStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(new GetCancellationRequestsQuery(status, page, pageSize), cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Approves a cancellation request: voids the Paymob payment, cancels the order and restores stock. Admin only.
        /// </summary>
        /// <param name="requestId">The cancellation request identifier.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>No content when approved.</returns>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "Admin")]
        [HttpPost("cancellation-requests/{requestId:guid}/approve")]
        public async Task<IActionResult> ApproveCancellationRequest(Guid requestId, CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(new ApproveCancellationRequestCommand(requestId), cancellationToken);
            return result.IsSuccess ? NoContent() : BadRequest(result.Errors);
        }

        /// <summary>
        /// Rejects a cancellation request and reverts the order to its previous status. Admin only.
        /// </summary>
        /// <param name="requestId">The cancellation request identifier.</param>
        /// <param name="command">The rejection note.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>No content when rejected.</returns>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "Admin")]
        [HttpPost("cancellation-requests/{requestId:guid}/reject")]
        public async Task<IActionResult> RejectCancellationRequest(Guid requestId, [FromBody] RejectCancellationRequestCommand command, CancellationToken cancellationToken = default)
        {
            if (requestId != command.RequestId)
                return BadRequest("The route request id must match the request id.");

            var result = await _sender.Send(command, cancellationToken);
            return result.IsSuccess ? NoContent() : BadRequest(result.Errors);
        }

        /// <summary>
        /// Lists a page of return requests, optionally filtered by status. Admin only.
        /// </summary>
        /// <param name="status">Optional status filter (Pending, Accepted, Received, Completed, Rejected).</param>
        /// <param name="page">The page number, starting at 1.</param>
        /// <param name="pageSize">The number of requests per page (1-100).</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A page of return requests with their items.</returns>
        [ProducesResponseType(typeof(ResponseResult<PagedResult<ReturnRequestListDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "Admin")]
        [HttpGet("return-requests")]
        public async Task<IActionResult> GetReturnRequests([FromQuery] ReturnRequestStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(new GetReturnRequestsQuery(status, page, pageSize), cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Accepts a return request and assigns a courier for pickup. Admin only.
        /// </summary>
        /// <param name="requestId">The return request identifier.</param>
        /// <param name="command">The courier name and phone.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>No content when accepted.</returns>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "Admin")]
        [HttpPost("return-requests/{requestId:guid}/accept")]
        public async Task<IActionResult> AcceptReturnRequest(Guid requestId, [FromBody] AcceptReturnRequestCommand command, CancellationToken cancellationToken = default)
        {
            if (requestId != command.RequestId)
                return BadRequest("The route request id must match the request id.");

            var result = await _sender.Send(command, cancellationToken);
            return result.IsSuccess ? NoContent() : BadRequest(result.Errors);
        }

        /// <summary>
        /// Marks returned items as received at the warehouse for inspection. Admin only.
        /// </summary>
        /// <param name="requestId">The return request identifier.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>No content when received.</returns>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "Admin")]
        [HttpPost("return-requests/{requestId:guid}/receive")]
        public async Task<IActionResult> ReceiveReturnRequest(Guid requestId, CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(new ReceiveReturnRequestCommand(requestId), cancellationToken);
            return result.IsSuccess ? NoContent() : BadRequest(result.Errors);
        }

        /// <summary>
        /// Rejects a return request. Admin only.
        /// </summary>
        /// <param name="requestId">The return request identifier.</param>
        /// <param name="command">The rejection note.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>No content when rejected.</returns>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "Admin")]
        [HttpPost("return-requests/{requestId:guid}/reject")]
        public async Task<IActionResult> RejectReturnRequest(Guid requestId, [FromBody] RejectReturnRequestCommand command, CancellationToken cancellationToken = default)
        {
            if (requestId != command.RequestId)
                return BadRequest("The route request id must match the request id.");

            var result = await _sender.Send(command, cancellationToken);
            return result.IsSuccess ? NoContent() : BadRequest(result.Errors);
        }

        /// <summary>
        /// Processes the refund for an inspected return via the Paymob Refund API. Admin only.
        /// </summary>
        /// <param name="requestId">The return request identifier.</param>
        /// <param name="command">Optional partial amount in cents; omit to refund the full refundable amount (returned items, plus shipping for merchant-defect returns).</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>No content when the refund is processed.</returns>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "Admin")]
        [HttpPost("return-requests/{requestId:guid}/process-refund")]
        public async Task<IActionResult> ProcessReturnRefund(Guid requestId, [FromBody] ProcessReturnRefundCommand command, CancellationToken cancellationToken = default)
        {
            if (requestId != command.RequestId)
                return BadRequest("The route request id must match the request id.");

            var result = await _sender.Send(command, cancellationToken);
            return result.IsSuccess ? NoContent() : BadRequest(result.Errors);
        }

        /// <summary>
        /// Deletes a full order and restores the products' stock.
        /// </summary>
        /// <param name="orderId">The order identifier.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>No content when the order is deleted.</returns>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [HttpDelete("{orderId:guid}")]
        public async Task<IActionResult> DeleteFullOrder(Guid orderId, CancellationToken cancellationToken = default)
        {
            await _sender.Send(new DeleteOrderCommand(orderId), cancellationToken);
            return NoContent();
        }

        /// <summary>
        /// Deletes a specific item from an order and restores the product's stock.
        /// </summary>
        /// <param name="orderId">The order identifier.</param>
        /// <param name="productId">The product identifier.</param>
        /// <param name="quantity">The quantity to remove.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>No content when the item is deleted.</returns>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [HttpDelete("{orderId:guid}/items/{productId:guid}")]
        public async Task<IActionResult> DeleteSpecificItemFromOrder(Guid orderId, Guid productId, [FromQuery] int quantity, CancellationToken cancellationToken = default)
        {
            await _sender.Send(new DeleteOrderItemCommand(orderId, productId, quantity), cancellationToken);
            return NoContent();
        }
    }
}
