using App.Domain.Enums;

namespace App.Application.Orders.Common
{
    public static class OrderStatusTransitions
    {
        private static readonly Dictionary<OrderStatus, IReadOnlyList<OrderStatus>> _allowedTransitions = new()
        {
            [OrderStatus.Pending] = [OrderStatus.Confirmed, OrderStatus.Cancelled],
            [OrderStatus.Confirmed] = [OrderStatus.Processing, OrderStatus.Cancelled],
            [OrderStatus.Processing] = [OrderStatus.Shipped, OrderStatus.Cancelled],
            [OrderStatus.Shipped] = [OrderStatus.OutForDelivery, OrderStatus.Delivered],
            [OrderStatus.OutForDelivery] = [OrderStatus.Delivered],
            [OrderStatus.Delivered] = [],
            [OrderStatus.Cancelled] = [],
            [OrderStatus.CancellationRequested] = []
        };

        public static bool CanTransition(OrderStatus from, OrderStatus to)
            => _allowedTransitions.TryGetValue(from, out IReadOnlyList<OrderStatus>? allowed) && allowed.Contains(to);
    }
}
