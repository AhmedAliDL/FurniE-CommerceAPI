using App.Application.Common.Interfaces.Repos;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Domain.Entities;
using App.Infrastructure.Persistance;
using App.Infrastructure.Repositories;

namespace App.Infrastructure.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        public UnitOfWork(AppDbContext context)
        {
            _context = context;
            CartItems = new BaseRepository<CartItem>(_context);
            Coupons = new BaseRepository<Coupon>(_context);
            Categories = new BaseRepository<Category>(_context);
            ShoppingCarts = new BaseRepository<ShoppingCart>(_context);
            Reviews = new BaseRepository<Review>(_context);
            Products = new BaseRepository<Product>(_context);
            ProductImages = new BaseRepository<ProductImage>(_context);
            Payments = new BaseRepository<Payment>(_context);
            PaymentTransactions = new BaseRepository<PaymentTransaction>(_context);
            CancellationRequests = new BaseRepository<CancellationRequest>(_context);
            ReturnRequests = new BaseRepository<ReturnRequest>(_context);
            ReturnItems = new BaseRepository<ReturnItem>(_context);
            OrderItems = new BaseRepository<OrderItem>(_context);
            Orders = new BaseRepository<Order>(_context);
            Notifications = new BaseRepository<Notification>(_context);
            AuditLogs = new BaseRepository<AuditLog>(_context);
            WishlistItems = new BaseRepository<WishlistItem>(_context);
            SupportTickets = new BaseRepository<SupportTicket>(_context);
            SupportConversations = new BaseRepository<SupportConversation>(_context);
            SupportMessages = new BaseRepository<SupportMessage>(_context);
            IdempotencyRecords = new BaseRepository<IdempotencyRecord>(_context);
        }
        public IBaseRepository<Notification> Notifications { get; private set; }
        public IBaseRepository<AuditLog> AuditLogs { get; private set; }
        public IBaseRepository<WishlistItem> WishlistItems { get; private set; }
        public IBaseRepository<SupportTicket> SupportTickets { get; private set; }
        public IBaseRepository<SupportConversation> SupportConversations { get; private set; }
        public IBaseRepository<SupportMessage> SupportMessages { get; private set; }
        public IBaseRepository<IdempotencyRecord> IdempotencyRecords { get; private set; }

        public IBaseRepository<CartItem> CartItems { get; private set; }

        public IBaseRepository<Coupon> Coupons { get; private set; }

        public IBaseRepository<Order> Orders { get; private set; }
        public IBaseRepository<OrderItem> OrderItems { get; private set; }

        public IBaseRepository<Payment> Payments { get; private set; }

        public IBaseRepository<PaymentTransaction> PaymentTransactions { get; private set; }

        public IBaseRepository<CancellationRequest> CancellationRequests { get; private set; }

        public IBaseRepository<ReturnRequest> ReturnRequests { get; private set; }

        public IBaseRepository<ReturnItem> ReturnItems { get; private set; }

        public IBaseRepository<Product> Products { get; private set; }

        public IBaseRepository<ProductImage> ProductImages { get; private set; }

        public IBaseRepository<Review> Reviews { get; private set; }
        public IBaseRepository<Category> Categories { get; private set; }

        public IBaseRepository<ShoppingCart> ShoppingCarts { get; private set; }


        public async Task<int> CompleteAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            _context.Dispose();
        }
    }
}
