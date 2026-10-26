using System.Text.Json;
using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Payments.Dto;
using App.Application.Payments.Dto.Paymob;
using App.Application.Responses;
using App.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace App.Application.Payments.Command.CreatePaymentSession
{
    public class CreatePaymentSessionHandler(IUnitOfWork unitOfWork, IPaymobService paymobService, ICurrentUserService currentUserService, IConfiguration configuration, ILogger<CreatePaymentSessionHandler> logger) : IRequestHandler<CreatePaymentSessionCommand, ResponseResult<PaymentSessionDto>>
    {
        private const int ExpirationSeconds = 3600;

        public async Task<ResponseResult<PaymentSessionDto>> Handle(CreatePaymentSessionCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Create payment session operation started. {OrderId}", request.OrderId);
            var userId = currentUserService.UserId ?? throw new NotFoundException("User not found.");

            var order = await unitOfWork.Orders.FindAsync(
                o => o.OrderId == request.OrderId,
                [o => o.Customer, o => o.OrderItems],
                cancellationToken)
                ?? throw new NotFoundException($"Order with id {request.OrderId} was not found.");

            if (order.UserId != userId)
                throw new ForbiddenException("You can only create a payment session for your own orders.");

            var existingPayment = await unitOfWork.Payments.FindAsync(p => p.OrderId == order.OrderId, cancellationToken);
            if (existingPayment?.PaymentStatus == "Completed")
                return ResponseResult<PaymentSessionDto>.Failure("This order has already been paid.");

            IdempotencyRecord? idempotencyRecord = null;
            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                idempotencyRecord = await unitOfWork.IdempotencyRecords.FindAsync(
                    r => r.UserId == userId && r.IdempotencyKey == request.IdempotencyKey, cancellationToken);
                if (idempotencyRecord != null)
                {
                    if (idempotencyRecord.OrderId != order.OrderId)
                        return ResponseResult<PaymentSessionDto>.Failure("This idempotency key was already used for a different order.");

                    var recordFreshAt = idempotencyRecord.UpdatedAt ?? idempotencyRecord.CreatedAt;
                    if (!string.IsNullOrWhiteSpace(idempotencyRecord.ResponsePayload)
                        && recordFreshAt > DateTime.UtcNow.AddSeconds(-ExpirationSeconds)
                        && JsonSerializer.Deserialize<PaymentSessionDto>(idempotencyRecord.ResponsePayload) is { } replay)
                    {
                        logger.LogInformation("Duplicate payment session request for idempotency key {IdempotencyKey}. Returning existing session. OrderId: {OrderId}", request.IdempotencyKey, order.OrderId);
                        return ResponseResult<PaymentSessionDto>.Success(replay);
                    }
                }
            }

            var productIds = order.OrderItems.Select(oi => oi.ProductId).Distinct().ToList();
            var products = (await unitOfWork.Products.FindAllAsync(p => productIds.Contains(p.ProductId), cancellationToken))
                .ToDictionary(p => p.ProductId);

            var intentionItems = order.OrderItems.Select(oi => new PaymobIntentionItem
            {
                Name = products.TryGetValue(oi.ProductId, out var product) ? product.ProductName : $"Product {oi.ProductId}",
                AmountCents = (long)Math.Round(oi.UnitPrice * 100m),
                Description = "Furni store product",
                Quantity = oi.Quantity
            }).ToList();

            if (order.ShippingFee > 0)
            {
                intentionItems.Add(new PaymobIntentionItem
                {
                    Name = "Shipping",
                    AmountCents = (long)Math.Round(order.ShippingFee * 100m),
                    Description = "Shipping fees",
                    Quantity = 1
                });
            }

            var intentionRequest = new PaymobIntentionRequest
            {
                AmountCents = (long)Math.Round(order.TotalPrice * 100m),
                Currency = "EGP",
                PaymentMethods = ParseIntegrationIds(configuration),
                Items = intentionItems,
                BillingData = MapBillingData(order.Customer),
                // Sent back to the webhook as order.merchant_order_id.
                SpecialReference = order.OrderId.ToString(),
                Expiration = ExpirationSeconds,
                NotificationUrl = configuration["Paymob:NotificationUrl"],
                RedirectionUrl = configuration["Paymob:RedirectionUrl"]
            };

            var intention = await paymobService.CreateIntentionAsync(intentionRequest, cancellationToken);

            var session = new PaymentSessionDto
            {
                IntentionId = intention.Id,
                ClientSecret = intention.ClientSecret,
                CheckoutUrl = paymobService.BuildCheckoutUrl(intention.ClientSecret),
                PaymentMethods = intention.PaymentMethods
            };

            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                var payload = JsonSerializer.Serialize(session);
                if (idempotencyRecord != null)
                {
                    idempotencyRecord.ResponsePayload = payload;
                    idempotencyRecord.UpdatedAt = DateTime.UtcNow;
                    unitOfWork.IdempotencyRecords.Update(idempotencyRecord);
                }
                else
                {
                    await unitOfWork.IdempotencyRecords.AddAsync(new IdempotencyRecord
                    {
                        UserId = userId,
                        IdempotencyKey = request.IdempotencyKey,
                        OrderId = order.OrderId,
                        ResponsePayload = payload
                    }, cancellationToken);
                }
            }

            try
            {
                await unitOfWork.CompleteAsync(cancellationToken);
            }
            catch (DbUpdateException) when (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                // A concurrent request with the same key won the unique-index race; replay its stored session.
                var winner = await unitOfWork.IdempotencyRecords.FindAsync(
                    r => r.UserId == userId && r.IdempotencyKey == request.IdempotencyKey, cancellationToken);
                if (winner != null
                    && !string.IsNullOrWhiteSpace(winner.ResponsePayload)
                    && JsonSerializer.Deserialize<PaymentSessionDto>(winner.ResponsePayload) is { } concurrentReplay)
                {
                    logger.LogInformation("Concurrent duplicate payment session detected for idempotency key {IdempotencyKey}. Returning existing session. OrderId: {OrderId}", request.IdempotencyKey, order.OrderId);
                    return ResponseResult<PaymentSessionDto>.Success(concurrentReplay);
                }

                throw;
            }

            logger.LogInformation("Create payment session operation completed. {OrderId}", order.OrderId);
            return ResponseResult<PaymentSessionDto>.Success(session);
        }

        private static List<int> ParseIntegrationIds(IConfiguration configuration) =>
            (configuration["Paymob:IntegrationsId"] ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(int.Parse)
                .ToList();

        private static PaymobBillingData MapBillingData(AppUser customer) => new()
        {
            FirstName = OrNa(customer.FName),
            LastName = OrNa(customer.LName),
            Email = OrNa(customer.Email),
            PhoneNumber = OrNa(customer.PhoneNumber),
            Street = OrNa(customer.Address),
            City = OrNa(customer.City),
            Country = OrNa(customer.Country),
            Apartment = "NA",
            Floor = "NA",
            Building = "NA",
            State = "NA"
        };

        private static string OrNa(string? value) =>
            string.IsNullOrWhiteSpace(value) ? "NA" : value;
    }
}
