using App.Application.Common.Interfaces.Repos;
using App.Domain.Entities;

namespace App.Application.Common.Interfaces.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        IBaseRepository<CartItem> CartItems { get; }
        IBaseRepository<Coupon> Coupons { get; }
        IBaseRepository<Category> Categories { get; }
        IBaseRepository<Order> Orders { get; }
        IBaseRepository<OrderItem> OrderItems { get; }
        IBaseRepository<Payment> Payments { get; }
        IBaseRepository<PaymentTransaction> PaymentTransactions { get; }
        IBaseRepository<CancellationRequest> CancellationRequests { get; }
        IBaseRepository<ReturnRequest> ReturnRequests { get; }
        IBaseRepository<ReturnItem> ReturnItems { get; }
        IBaseRepository<Product> Products { get; }
        IBaseRepository<ProductImage> ProductImages { get; }
        IBaseRepository<Review> Reviews { get; }
        IBaseRepository<ShoppingCart> ShoppingCarts { get; }
        IBaseRepository<Notification> Notifications { get; }
        IBaseRepository<AuditLog> AuditLogs { get; }
        IBaseRepository<WishlistItem> WishlistItems { get; }
        IBaseRepository<SupportTicket> SupportTickets { get; }
        IBaseRepository<SupportConversation> SupportConversations { get; }
        IBaseRepository<SupportMessage> SupportMessages { get; }
        IBaseRepository<IdempotencyRecord> IdempotencyRecords { get; }
        Task<int> CompleteAsync(CancellationToken cancellationToken = default);
    }

}
