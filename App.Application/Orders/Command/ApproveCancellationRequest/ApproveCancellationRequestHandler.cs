using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Responses;
using App.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Orders.Command.ApproveCancellationRequest
{
    public class ApproveCancellationRequestHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IPaymobService paymobService, INotificationService notificationService, ILogger<ApproveCancellationRequestHandler> logger) : IRequestHandler<ApproveCancellationRequestCommand, ResponseResult>
    {
        public async Task<ResponseResult> Handle(ApproveCancellationRequestCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Cancellation request approval started. RequestId: {RequestId}", request.RequestId);

            Guid adminId = currentUserService.UserId ?? throw new NotFoundException("User not found.");

            var cancellationRequest = await unitOfWork.CancellationRequests.FindAsync(
                r => r.Id == request.RequestId,
                [r => r.Order],
                cancellationToken)
                ?? throw new NotFoundException($"Cancellation request with id {request.RequestId} was not found.");

            if (cancellationRequest.Status != CancellationRequestStatus.Pending)
                return ResponseResult.Failure($"Cancellation request is already {cancellationRequest.Status}.");

            var transactions = await unitOfWork.PaymentTransactions.FindAllAsync(t => t.OrderId == cancellationRequest.OrderId, cancellationToken);
            var transaction = transactions
                .Where(t => t.Status == "Success")
                .OrderByDescending(t => t.PaymobCreatedAt)
                .FirstOrDefault();
            if (transaction == null)
                return ResponseResult.Failure("No successful payment found for this order. Use the direct cancel or refund flow instead.");

            // Paymob rejects the void once the transaction settles; nothing else enforces a window here.
            var result = await paymobService.VoidTransactionAsync(transaction.PaymobTransactionId, cancellationToken);
            if (!result.Success)
                return ResponseResult.Failure(result.Message ?? "Paymob rejected the void. The payment may already be settled; use the refund flow instead.");

            transaction.Status = "Voided";
            transaction.UpdatedAt = DateTime.UtcNow;
            unitOfWork.PaymentTransactions.Update(transaction);

            var payment = await unitOfWork.Payments.FindAsync(p => p.OrderId == cancellationRequest.OrderId, cancellationToken);
            if (payment != null)
            {
                payment.PaymentStatus = "Voided";
                payment.UpdatedAt = DateTime.UtcNow;
                unitOfWork.Payments.Update(payment);
            }

            cancellationRequest.Status = CancellationRequestStatus.Approved;
            cancellationRequest.ProcessedAt = DateTime.UtcNow;
            cancellationRequest.UpdatedAt = DateTime.UtcNow;
            unitOfWork.CancellationRequests.Update(cancellationRequest);

            var order = cancellationRequest.Order;
            order.OrderStatus = OrderStatus.Cancelled;
            order.UpdatedAt = DateTime.UtcNow;
            unitOfWork.Orders.Update(order);

            var orderItems = await unitOfWork.OrderItems.FindAllAsync(oi => oi.OrderId == order.OrderId, [oi => oi.Product], cancellationToken);
            foreach (var orderItem in orderItems)
            {
                bool wasOutOfStock = orderItem.Product.StockQuantity == 0;
                orderItem.Product.StockQuantity += orderItem.Quantity;
                if (wasOutOfStock)
                    orderItem.Product.IsActive = true;

                unitOfWork.Products.Update(orderItem.Product);
            }

            await unitOfWork.CompleteAsync(cancellationToken);

            await notificationService.NotifyUserAsync(order.UserId,
                $"Your cancellation request for order {order.OrderId} was approved and the payment hold was released.",
                adminId,
                $"api/orders/{order.OrderId}/details",
                cancellationToken);

            logger.LogInformation("Cancellation request approved. RequestId: {RequestId}, OrderId: {OrderId}", request.RequestId, order.OrderId);
            return ResponseResult.Success();
        }
    }
}
