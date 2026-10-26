using App.Application.Common.Enums;
using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Orders.Dto;
using App.Application.Responses;
using App.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Orders.Queries.GetUserOrders
{
    public class GetUserOrdersHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, ILogger<GetUserOrdersHandler> logger) : IRequestHandler<GetUserOrdersQuery, PagedResult<OrderShippingDto>>
    {
        private const int _totalDaysForSendingOrder = 5;

        public async Task<PagedResult<OrderShippingDto>> Handle(GetUserOrdersQuery request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Get user orders operation started.");
            Guid userId = currentUserService.UserId ?? throw new NotFoundException("User not found.");

            (IReadOnlyList<Order> ordersForUser, int totalCount) = await unitOfWork.Orders.FindPagedAsync(
                o => o.UserId == userId,
                [],
                SortDirection.Descending,
                o => o.CreatedAt,
                request.Page,
                request.PageSize,
                cancellationToken);

            var orderIds = ordersForUser.Select(o => o.OrderId).ToList();
            Dictionary<Guid, int> totalQuantitiesByOrder;
            if (orderIds.Count == 0)
            {
                totalQuantitiesByOrder = [];
            }
            else
            {
                var orderItems = await unitOfWork.OrderItems.FindAllAsync(oi => orderIds.Contains(oi.OrderId), cancellationToken);
                totalQuantitiesByOrder = orderItems
                    .GroupBy(oi => oi.OrderId)
                    .ToDictionary(g => g.Key, g => g.Sum(oi => oi.Quantity));
            }

            var items = ordersForUser.Select(order =>
            {
                TimeSpan remainingTime = order.CreatedAt.AddDays(_totalDaysForSendingOrder) - DateTime.UtcNow;
                if (remainingTime.TotalDays < 0)
                    remainingTime = TimeSpan.Zero;

                return new OrderShippingDto
                {
                    OrderId = order.OrderId,
                    Address = order.ShippingAddress,
                    OrderDate = order.CreatedAt.Date,
                    ShippingStatus = order.OrderStatus.ToString(),
                    TotalQuantity = totalQuantitiesByOrder.GetValueOrDefault(order.OrderId),
                    TotalPrice = order.TotalPrice,
                    RemainingDays = (int)Math.Ceiling(remainingTime.TotalDays)
                };
            }).ToList();

            logger.LogInformation("Get user orders operation completed. TotalCount: {TotalCount}", totalCount);
            return new PagedResult<OrderShippingDto>
            {
                Items = items,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }
    }
}
