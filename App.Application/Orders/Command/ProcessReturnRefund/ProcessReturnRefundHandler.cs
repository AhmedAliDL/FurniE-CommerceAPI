using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Payments.Command.RefundPayment;
using App.Application.Responses;
using App.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Orders.Command.ProcessReturnRefund
{
    public class ProcessReturnRefundHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, ISender sender, INotificationService notificationService, ILogger<ProcessReturnRefundHandler> logger) : IRequestHandler<ProcessReturnRefundCommand, ResponseResult>
    {
        public async Task<ResponseResult> Handle(ProcessReturnRefundCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Return refund processing started. RequestId: {RequestId}", request.RequestId);

            Guid adminId = currentUserService.UserId ?? throw new NotFoundException("User not found.");

            var returnRequest = await unitOfWork.ReturnRequests.FindAsync(
                r => r.Id == request.RequestId,
                [r => r.Order, r => r.ReturnItems],
                cancellationToken)
                ?? throw new NotFoundException($"Return request with id {request.RequestId} was not found.");

            if (returnRequest.Status != ReturnRequestStatus.Received)
                return ResponseResult.Failure($"Return request must be received and inspected before a refund can be processed (current: {returnRequest.Status}).");

            // Returned items are refunded pro-rata by their ordered unit price.
            long itemsCents = 0;
            foreach (var returnItem in returnRequest.ReturnItems)
            {
                var orderItem = await unitOfWork.OrderItems.FindAsync(oi => oi.OrderItemId == returnItem.OrderItemId, cancellationToken);
                if (orderItem == null)
                    return ResponseResult.Failure($"Order item {returnItem.OrderItemId} no longer exists.");

                itemsCents += (long)Math.Round(orderItem.UnitPrice * 100m) * returnItem.Quantity;
            }

            // Shipping fees are refunded only when the return is due to a merchant defect.
            long shippingCents = returnRequest.ReasonCategory == ReturnReasonCategory.MerchantDefect
                ? (long)Math.Round(returnRequest.Order.ShippingFee * 100m)
                : 0;

            var capCents = itemsCents + shippingCents;
            var refundCents = request.AmountCents ?? capCents;
            if (refundCents <= 0 || refundCents > capCents)
            {
                var capExplanation = returnRequest.ReasonCategory == ReturnReasonCategory.MerchantDefect
                    ? "returned items plus shipping fees"
                    : "returned items only (shipping fees are excluded for customer-choice returns)";
                return ResponseResult.Failure($"Refund amount must be between 1 and {capCents} cents ({capExplanation}).");
            }

            var refundResult = await sender.Send(new RefundPaymentCommand { OrderId = returnRequest.OrderId, AmountCents = refundCents }, cancellationToken);
            if (!refundResult.IsSuccess)
                return ResponseResult.Failure([.. refundResult.Errors]);

            returnRequest.Status = ReturnRequestStatus.Completed;
            returnRequest.RefundAmountCents = refundCents;
            returnRequest.ProcessedAt = DateTime.UtcNow;
            returnRequest.UpdatedAt = DateTime.UtcNow;
            unitOfWork.ReturnRequests.Update(returnRequest);

            await unitOfWork.CompleteAsync(cancellationToken);

            await notificationService.NotifyUserAsync(returnRequest.CustomerId,
                $"Your refund for order {returnRequest.OrderId} was processed ({refundCents / 100m:0.00} EGP).",
                adminId,
                $"api/orders/{returnRequest.OrderId}/details",
                cancellationToken);

            logger.LogInformation("Return refund processed. RequestId: {RequestId}, RefundCents: {RefundCents}", request.RequestId, refundCents);
            return ResponseResult.Success();
        }
    }
}
