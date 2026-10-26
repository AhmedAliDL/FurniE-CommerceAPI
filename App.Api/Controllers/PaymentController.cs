using App.Application.Payments.Command.CreatePaymentSession;
using App.Application.Payments.Command.HandlePaymobCallback;
using App.Application.Payments.Command.RefundPayment;
using App.Application.Payments.Command.VoidPayment;
using App.Application.Payments.Dto;
using App.Application.Payments.Dto.Paymob;
using App.Application.Payments.Queries.GetPaymentTransactions;
using App.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Api.Controllers
{
    /// <summary>
    /// Handles payment sessions, Paymob callbacks, transaction listing, refunds, and payment voids.
    /// </summary>
    [ApiController]
    [Route("api/payments")]
    [Authorize]
    public class PaymentController : BaseController
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PaymentController"/> class.
        /// </summary>
        /// <param name="sender">The MediatR sender used to dispatch commands and queries.</param>
        public PaymentController(ISender sender) : base(sender)
        {
        }
        /// <summary>
        /// Creates a Paymob payment session for one of the current user's orders.
        /// </summary>
        /// <param name="command">The order to pay for, plus an optional idempotency key: repeating the same key for the same order within the intention's lifetime replays the existing session instead of creating a new Paymob intention.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>The client secret, checkout URL and available payment methods.</returns>
        [ProducesResponseType(typeof(ResponseResult<PaymentSessionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpPost("session")]
        public async Task<IActionResult> CreateSession([FromBody] CreatePaymentSessionCommand command, CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(command, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Receives Paymob transaction callbacks. The HMAC signature arrives as a query string.
        /// </summary>
        /// <param name="body">The transaction callback payload.</param>
        /// <param name="hmac">The HMAC signature computed by Paymob.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [AllowAnonymous]
        [HttpPost("callback")]
        public async Task<IActionResult> Callback([FromBody] PaymobCallbackDto body, [FromQuery] string? hmac, CancellationToken cancellationToken = default)
        {
            await _sender.Send(new HandlePaymobCallbackCommand(body, hmac), cancellationToken);
            return Ok(new { message = "Callback processed." });
        }

        /// <summary>
        /// Gets the recorded Paymob transactions (admin only).
        /// </summary>
        /// <param name="query">Optional order filter and paging.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A page of payment transactions.</returns>
        [ProducesResponseType(typeof(PagedResult<PaymentTransactionResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [Authorize(Roles = "Admin")]
        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions([FromQuery] GetPaymentTransactionsQuery query, CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Gets every recorded Paymob transaction without requiring an order id (admin only).
        /// </summary>
        /// <param name="page">The page number, starting at 1.</param>
        /// <param name="pageSize">The number of transactions per page.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A page of payment transactions, newest first.</returns>
        [ProducesResponseType(typeof(PagedResult<PaymentTransactionResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [Authorize(Roles = "Admin")]
        [HttpGet("transactions/all")]
        public async Task<IActionResult> GetAllTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(
                new GetPaymentTransactionsQuery { Page = page, PageSize = pageSize },
                cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Refunds (fully or partially) the payment of a delivered order. Refunds are only
        /// allowed within 14 days after delivery.
        /// </summary>
        /// <param name="command">The order to refund. Omit AmountCents to refund everything still refundable.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>The updated transaction status and refunded amount.</returns>
        [ProducesResponseType(typeof(ResponseResult<PaymentOperationResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Roles = "Admin")]
        [HttpPost("refund")]
        public async Task<IActionResult> Refund([FromBody] RefundPaymentCommand command, CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(command, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Voids the payment of an order. Voids are only allowed within 24 hours of payment.
        /// </summary>
        /// <param name="command">The order whose payment should be voided.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>The updated transaction status.</returns>
        [ProducesResponseType(typeof(ResponseResult<PaymentOperationResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Roles = "Admin")]
        [HttpPost("void")]
        public async Task<IActionResult> Void([FromBody] VoidPaymentCommand command, CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(command, cancellationToken);
            return Ok(result);
        }
    }
}
