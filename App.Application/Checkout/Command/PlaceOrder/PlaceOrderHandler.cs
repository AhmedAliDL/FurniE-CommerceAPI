using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Responses;
using App.Domain.Entities;
using App.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Application.Checkout.Command.PlaceOrder
{
    public class PlaceOrderHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IIdentityService identityService, IShippingRateService shippingRateService, ILogger<PlaceOrderHandler> logger) : IRequestHandler<PlaceOrderCommand, ResponseResult<Guid>>
    {
        public async Task<ResponseResult<Guid>> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Place order operation started.");
            Guid userId = currentUserService.UserId ?? throw new NotFoundException("User not found.");

            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                var processed = await unitOfWork.IdempotencyRecords.FindAsync(r => r.UserId == userId && r.IdempotencyKey == request.IdempotencyKey, cancellationToken);
                if (processed != null)
                {
                    logger.LogInformation("Duplicate checkout request for idempotency key {IdempotencyKey}. Returning existing order {OrderId}.", request.IdempotencyKey, processed.OrderId);
                    return ResponseResult<Guid>.Success(processed.OrderId.GetValueOrDefault());
                }
            }

            var user = await identityService.GetUserByIdAsync(userId) ?? throw new NotFoundException("User not found.");

            var shoppingCart = await unitOfWork.ShoppingCarts.FindAsync(c => c.UserId == userId, cancellationToken)
                ?? throw new NotFoundException("Shopping cart not found.");

            var cartItems = (await unitOfWork.CartItems.FindAllAsync(ci => ci.ShoppingCartId == shoppingCart.Id, [ci => ci.Product], cancellationToken)).ToList();

            if (cartItems.Count == 0)
                return ResponseResult<Guid>.Failure("The shopping cart is empty.");

            // The discount is resolved from the coupon on the server; a client-computed percentage is never trusted.
            decimal discountPercent = 0m;
            if (!string.IsNullOrWhiteSpace(request.CouponCode))
            {
                string couponCode = request.CouponCode.Trim();
                var coupon = await unitOfWork.Coupons.FindAsync(c => c.CouponCode == couponCode, cancellationToken);
                if (coupon == null)
                    return ResponseResult<Guid>.Failure("Coupon not found.");

                if (coupon.ExpireDate < DateTime.Now)
                    return ResponseResult<Guid>.Failure("Coupon has expired.");

                discountPercent = coupon.CouponRatio;
            }
            else if (request.OrderDiscount != 0m)
            {
                return ResponseResult<Guid>.Failure("A valid coupon code is required to apply a discount.");
            }

            var orderItems = new List<OrderItem>();
            foreach (var cartItem in cartItems)
            {
                if (cartItem.Product.StockQuantity < cartItem.ItemQuantity)
                    return ResponseResult<Guid>.Failure($"Product {cartItem.Product.ProductName} is out of stock.");

                decimal unitPrice = ApplyCouponDiscount(cartItem.Product.Price, discountPercent);
                orderItems.Add(new OrderItem
                {
                    ProductId = cartItem.ProductId,
                    Quantity = cartItem.ItemQuantity,
                    UnitPrice = unitPrice
                });

                cartItem.Product.StockQuantity -= cartItem.ItemQuantity;
                if (cartItem.Product.StockQuantity == 0) cartItem.Product.IsActive = false;
                unitOfWork.Products.Update(cartItem.Product);

                unitOfWork.CartItems.Delete(cartItem);
            }

            // The total is computed server-side so shipping fees cannot be tampered with; request.TotalPrice is ignored.
            decimal itemsSubtotal = orderItems.Sum(oi => oi.UnitPrice * oi.Quantity);
            string city = !string.IsNullOrWhiteSpace(request.City) ? request.City : user.City;
            decimal shippingFee = shippingRateService.GetShippingFee(city);

            var order = new Order
            {
                UserId = userId,
                TotalPrice = itemsSubtotal + shippingFee,
                ShippingFee = shippingFee,
                OrderStatus = OrderStatus.Processing,
                ShippingAddress = request.Address ?? user.Address,
                CouponDiscount = discountPercent
            };
            // The order id is generated on Add, so the items connect through the navigation.
            order.OrderItems = orderItems;
            await unitOfWork.Orders.AddAsync(order, cancellationToken);
            await unitOfWork.OrderItems.AddRangeAsync(orderItems, cancellationToken);

            var payment = new Payment
            {
                OrderId = order.OrderId,
                PaymentMethod = request.PaymentMethod,
                Amount = (double)(itemsSubtotal + shippingFee),
                PaymentStatus = CalculatePaymentStatus(orderItems.Count)
            };
            await unitOfWork.Payments.AddAsync(payment, cancellationToken);

            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                await unitOfWork.IdempotencyRecords.AddAsync(new IdempotencyRecord
                {
                    UserId = userId,
                    IdempotencyKey = request.IdempotencyKey,
                    OrderId = order.OrderId
                }, cancellationToken);
            }

            try
            {
                await unitOfWork.CompleteAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return ResponseResult<Guid>.Failure("Product stock changed while placing your order. Please review your cart and try again.");
            }
            catch (DbUpdateException) when (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                var processed = await unitOfWork.IdempotencyRecords.FindAsync(r => r.UserId == userId && r.IdempotencyKey == request.IdempotencyKey, cancellationToken);
                if (processed != null)
                {
                    logger.LogInformation("Concurrent duplicate checkout detected for idempotency key {IdempotencyKey}. Returning existing order {OrderId}.", request.IdempotencyKey, processed.OrderId);
                    return ResponseResult<Guid>.Success(processed.OrderId.GetValueOrDefault());
                }

                throw;
            }

            logger.LogInformation("Place order operation completed.");
            return ResponseResult<Guid>.Success(order.OrderId);
        }

        private static decimal ApplyCouponDiscount(decimal price, decimal discountPercent) =>
            discountPercent == 0m ? price : price - (price * discountPercent / 100m);

        // An order is never paid at placement: Visa completes through the Paymob callback,
        // and Cash completes on delivery.
        private static string CalculatePaymentStatus(int orderItems) =>
            orderItems > 0 ? "Pending" : "Cancelled";
    }
}
