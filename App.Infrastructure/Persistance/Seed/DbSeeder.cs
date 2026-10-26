using System.Text.Json;
using App.Application.Common.Authorization;
using App.Domain.Entities;
using App.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace App.Infrastructure.Persistance.Seed
{
    /// <summary>
    /// Seeds a demo-ready Furni dataset: admin, support agent, and customer accounts,
    /// the furniture catalog, live carts and wishlists, orders in every status,
    /// payments, returns, support conversations, notifications, and idempotency records.
    /// Audit logs and refresh tokens are intentionally excluded because the running
    /// application produces them.
    /// </summary>
    public static class DbSeeder
    {
        private const string DemoPassword = "Furni@2026";
        private const string CatalogMarker = "Nordic 3-Seater Linen Sofa";

        private sealed record DemoUser(
            string Key,
            string Email,
            string FirstName,
            string LastName,
            string City,
            string Address,
            string Phone,
            string Role);

        private static readonly DemoUser[] DemoUsers =
        [
            new("admin", "admin@furni.test", "Ahmed", "Hassan", "Cairo",
                "12 Tahrir Square, Downtown", "+201000000101", SupportRoles.Admin),
            new("ops", "sara.ops@furni.test", "Sara", "El-Masry", "Giza",
                "8 El-Nile Street, Dokki", "+201000000102", SupportRoles.Admin),
            new("agent1", "nour.support@furni.test", "Nour", "Adel", "Cairo",
                "27 Makram Ebeid Street, Nasr City", "+201000000201", SupportRoles.SupportAgent),
            new("agent2", "karim.support@furni.test", "Karim", "Fouad", "Alexandria",
                "15 El-Horreya Road, Roushdy", "+201000000202", SupportRoles.SupportAgent),
            new("mariam", "mariam@furni.test", "Mariam", "Youssef", "New Cairo",
                "Villa 14, 90th Street, Fifth Settlement", "+201000000301", SupportRoles.Customer),
            new("omar", "omar@furni.test", "Omar", "Khaled", "Alexandria",
                "22 Sidi Gaber Street, Smouha", "+201000000302", SupportRoles.Customer),
            new("laila", "laila@furni.test", "Laila", "Ibrahim", "Giza",
                "5 El-Haram Street, Faisal", "+201000000303", SupportRoles.Customer),
            new("youssef", "youssef@furni.test", "Youssef", "Mansour", "Sheikh Zayed",
                "Townhouse 9, Beverly Hills", "+201000000304", SupportRoles.Customer)
        ];

        public static async Task SeedAsync(IServiceProvider services)
        {
            var context = services.GetRequiredService<AppDbContext>();
            var userManager = services.GetRequiredService<UserManager<AppUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

            try
            {
                await EnsureRolesAsync(roleManager);
                var users = await EnsureDemoUsersAsync(userManager, logger);

                if (await context.Products.AnyAsync(p => p.ProductName == CatalogMarker))
                {
                    logger.LogInformation("Demo seed skipped: the Furni demo catalog is already present.");
                    return;
                }

                await SeedStoreAsync(context, users);
                logger.LogInformation("Demo seed completed: {Users} accounts, catalog, carts, orders, payments, returns, and support data created.", users.Count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Demo data seeding failed. The application will continue without demo data.");
            }
        }

        private static async Task EnsureRolesAsync(RoleManager<IdentityRole<Guid>> roleManager)
        {
            foreach (var role in new[] { SupportRoles.Admin, SupportRoles.SupportAgent, SupportRoles.Customer })
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        private static async Task<Dictionary<string, AppUser>> EnsureDemoUsersAsync(
            UserManager<AppUser> userManager,
            ILogger logger)
        {
            var users = new Dictionary<string, AppUser>(StringComparer.OrdinalIgnoreCase);

            foreach (var demo in DemoUsers)
            {
                var user = await userManager.FindByEmailAsync(demo.Email);
                if (user is null)
                {
                    user = new AppUser
                    {
                        UserName = demo.Email,
                        Email = demo.Email,
                        EmailConfirmed = true,
                        PhoneNumber = demo.Phone,
                        PhoneNumberConfirmed = true,
                        FName = demo.FirstName,
                        LName = demo.LastName,
                        Country = "Egypt",
                        City = demo.City,
                        Address = demo.Address,
                        IsActive = true,
                        LastLoginAt = DateTime.UtcNow.AddDays(-1)
                    };

                    var created = await userManager.CreateAsync(user, DemoPassword);
                    if (!created.Succeeded)
                        throw new InvalidOperationException(
                            $"Could not create demo user {demo.Email}: {string.Join("; ", created.Errors.Select(e => e.Description))}");

                    logger.LogInformation("Created demo {Role} account {Email}.", demo.Role, demo.Email);
                }

                if (!await userManager.IsInRoleAsync(user, demo.Role))
                {
                    var assigned = await userManager.AddToRoleAsync(user, demo.Role);
                    if (!assigned.Succeeded)
                        throw new InvalidOperationException(
                            $"Could not assign role {demo.Role} to {demo.Email}: {string.Join("; ", assigned.Errors.Select(e => e.Description))}");
                }

                users[demo.Key] = user;
            }

            return users;
        }

        private static async Task SeedStoreAsync(AppDbContext context, Dictionary<string, AppUser> users)
        {
            var products = await AddCatalogAsync(context);
            AddCoupons(context);
            await AddCartsAsync(context, users, products);
            AddWishlists(context, users, products);

            var orders = AddOrders(context, users, products);
            AddCancellationRequests(context, users, orders);
            AddReturnRequests(context, users, orders);
            AddReviews(context, users, products);
            AddSupportTickets(context, users);
            AddNotifications(context, users, orders);
            AddIdempotencyRecords(context, users, orders);

            await context.SaveChangesAsync();
        }

        private static async Task<Dictionary<string, Product>> AddCatalogAsync(AppDbContext context)
        {
            var existingCategories = await context.Categories.ToDictionaryAsync(c => c.CategoryName, c => c);
            var newCategories = new List<Category>();

            Category Category(string name)
            {
                if (existingCategories.TryGetValue(name, out var existing))
                    return existing;

                var created = new Category { CategoryName = name };
                existingCategories[name] = created;
                newCategories.Add(created);
                return created;
            }

            var sofas = Category("Sofas & Couches");
            var chairs = Category("Armchairs & Chairs");
            var tables = Category("Tables & Desks");
            var beds = Category("Beds & Mattresses");
            var storage = Category("Storage & Shelving");
            var lighting = Category("Lighting");
            var decor = Category("Home Decor");
            var outdoor = Category("Outdoor Living");

            var products = new List<Product>
            {
                CreateProduct(sofas, "Nordic 3-Seater Linen Sofa", "nordic-linen-sofa", 24_500m, 8,
                    "A clean-lined three-seater in stone-washed linen with deep foam cushions and solid oak legs."),
                CreateProduct(sofas, "Milano Leather Corner Sofa", "milano-leather-sofa", 38_900m, 4,
                    "Top-grain leather corner sofa with a chaise, stitched seams, and a low contemporary profile."),
                CreateProduct(sofas, "Cloud Compact 2-Seater Sofa", "cloud-compact-sofa", 16_750m, 12,
                    "A soft two-seater with rounded arms and feather-blend cushions, sized for compact living rooms."),
                CreateProduct(chairs, "Oslo Velvet Accent Chair", "oslo-velvet-chair", 6_450m, 15,
                    "A velvet accent chair with a curved back and tapered brass-tipped legs."),
                CreateProduct(chairs, "Rattan Reading Chair", "rattan-reading-chair", 4_900m, 20,
                    "A hand-woven rattan reading chair with a generous seat and a honey-toned natural finish."),
                CreateProduct(chairs, "Executive Recliner Armchair", "executive-recliner", 12_300m, 3,
                    "A full-grain leather recliner with a padded headrest and a smooth manual recline."),
                CreateProduct(tables, "Oakridge 6-Seater Dining Table", "oakridge-dining-table", 18_700m, 7,
                    "A solid oak dining table that seats six, finished with a matte protective oil."),
                CreateProduct(tables, "Minimalist Walnut Study Desk", "walnut-study-desk", 7_250m, 18,
                    "A walnut-veneer study desk with a cable channel and two soft-close drawers."),
                CreateProduct(tables, "Marble Top Coffee Table", "marble-coffee-table", 9_800m, 10,
                    "A honed marble top on a brushed steel frame, with a lower shelf for books and trays."),
                CreateProduct(beds, "Aurora Upholstered King Bed", "aurora-king-bed", 27_400m, 5,
                    "An upholstered king bed with a channel-stitched headboard and a sturdy slatted base."),
                CreateProduct(beds, "SleepWell Queen Mattress", "sleepwell-queen-mattress", 14_900m, 16,
                    "A medium-firm queen mattress with pocket springs and a breathable knitted cover."),
                CreateProduct(beds, "Scandinavian Single Bed Frame", "scandinavian-single-bed", 9_600m, 0,
                    "A single bed frame in pale oak with a low headboard and under-bed clearance for storage.", isActive: false),
                CreateProduct(storage, "Cedar 6-Door Wardrobe", "cedar-wardrobe", 21_500m, 5,
                    "A six-door cedar wardrobe with soft-close hinges, two hanging rails, and six drawers."),
                CreateProduct(storage, "Industrial 5-Tier Bookshelf", "industrial-bookshelf", 6_800m, 22,
                    "A five-tier bookshelf combining black steel uprights with warm acacia shelves."),
                CreateProduct(storage, "Bedside Drawer Unit", "bedside-drawer-unit", 2_950m, 30,
                    "A two-drawer bedside unit with a solid top and an integrated cable notch."),
                CreateProduct(lighting, "Brass Arc Floor Lamp", "brass-arc-lamp", 4_600m, 14,
                    "A slim brass floor lamp with an adjustable arc and a weighted marble base."),
                CreateProduct(lighting, "Ceramic Table Lamp Set", "ceramic-lamp-set", 2_150m, 25,
                    "A set of two ceramic table lamps with linen shades in sand and charcoal."),
                CreateProduct(lighting, "Pendant Cluster Light", "pendant-cluster-light", 5_900m, 11,
                    "A cluster pendant light with three mouth-blown glass shades and adjustable cords."),
                CreateProduct(decor, "Handwoven Wool Area Rug", "wool-area-rug", 8_400m, 13,
                    "A handwoven wool rug with a subtle geometric pattern and a low, durable pile."),
                CreateProduct(decor, "Terracotta Vase Trio", "terracotta-vase-trio", 1_250m, 40,
                    "A set of three terracotta vases in graduated sizes with a matte finish."),
                CreateProduct(decor, "Framed Abstract Wall Art", "abstract-wall-art", 3_100m, 17,
                    "A framed abstract print in muted earth tones, mounted on archival paper."),
                CreateProduct(outdoor, "Teak Patio Dining Set", "teak-patio-set", 29_900m, 4,
                    "A four-seat teak dining set with a slatted table and weather-resistant cushions."),
                CreateProduct(outdoor, "Weatherproof Lounge Chair", "weatherproof-lounge-chair", 7_800m, 12,
                    "A powder-coated aluminium lounge chair with quick-dry cushions and a rocker base."),
                CreateProduct(outdoor, "Hanging Egg Garden Chair", "hanging-egg-chair", 10_500m, 8,
                    "A suspended egg chair in weatherproof wicker with a plush cushion and steel stand.")
            };

            if (newCategories.Count > 0)
                context.Categories.AddRange(newCategories);
            context.Products.AddRange(products);

            return products.ToDictionary(p => p.ProductName, StringComparer.OrdinalIgnoreCase);
        }

        private static Product CreateProduct(
            Category category,
            string name,
            string slug,
            decimal price,
            int stock,
            string description,
            bool isActive = true)
        {
            var primaryImage = ImageUrl(slug, 1);

            return new Product
            {
                ProductName = name,
                Description = description,
                Price = price,
                StockQuantity = stock,
                Category = category,
                ImageUrl = primaryImage,
                IsActive = isActive,
                ProductImages = new List<ProductImage>
                {
                    new() { ImageUrl = primaryImage, IsPrimary = true, SortOrder = 0 },
                    new() { ImageUrl = ImageUrl(slug, 2), SortOrder = 1 },
                    new() { ImageUrl = ImageUrl(slug, 3), SortOrder = 2 }
                }
            };
        }

        private static string ImageUrl(string slug, int index) =>
            $"https://picsum.photos/seed/furni-{slug}-{index}/1200/900";

        private static void AddCoupons(AppDbContext context)
        {
            var now = DateTime.UtcNow;

            context.Coupons.AddRange(
                new Coupon { CouponCode = "WELCOME10", CouponRatio = 10m, ExpireDate = now.AddDays(400) },
                new Coupon { CouponCode = "FURNI20", CouponRatio = 20m, ExpireDate = now.AddDays(220) },
                new Coupon { CouponCode = "VIP25", CouponRatio = 25m, ExpireDate = now.AddDays(120) },
                new Coupon { CouponCode = "SUMMER15", CouponRatio = 15m, ExpireDate = now.AddDays(-30) });
        }

        private static async Task AddCartsAsync(
            AppDbContext context,
            Dictionary<string, AppUser> users,
            Dictionary<string, Product> products)
        {
            var usersWithCarts = (await context.ShoppingCart.Select(c => c.UserId).ToListAsync()).ToHashSet();

            AddCart(context, users["mariam"], usersWithCarts,
                (products["Oslo Velvet Accent Chair"], 1),
                (products["Terracotta Vase Trio"], 2));

            AddCart(context, users["omar"], usersWithCarts,
                (products["Oakridge 6-Seater Dining Table"], 1),
                (products["Ceramic Table Lamp Set"], 1),
                (products["Handwoven Wool Area Rug"], 1));

            AddCart(context, users["laila"], usersWithCarts,
                (products["Cloud Compact 2-Seater Sofa"], 1));

            AddCart(context, users["youssef"], usersWithCarts);
        }

        private static void AddCart(
            AppDbContext context,
            AppUser user,
            HashSet<Guid> usersWithCarts,
            params (Product Product, int Quantity)[] items)
        {
            if (usersWithCarts.Contains(user.Id))
                return;

            context.ShoppingCart.Add(new ShoppingCart
            {
                Customer = user,
                ProductsCartItems = items
                    .Select(item => new CartItem { Product = item.Product, ItemQuantity = item.Quantity })
                    .ToList()
            });
        }

        private static void AddWishlists(
            AppDbContext context,
            Dictionary<string, AppUser> users,
            Dictionary<string, Product> products)
        {
            AddWishlistItem(context, users["mariam"], products["Milano Leather Corner Sofa"], 41_900m);
            AddWishlistItem(context, users["mariam"], products["Brass Arc Floor Lamp"]);
            AddWishlistItem(context, users["mariam"], products["Teak Patio Dining Set"], 31_500m);

            AddWishlistItem(context, users["omar"], products["Aurora Upholstered King Bed"]);
            AddWishlistItem(context, users["omar"], products["Cedar 6-Door Wardrobe"]);

            AddWishlistItem(context, users["laila"], products["Hanging Egg Garden Chair"]);
            AddWishlistItem(context, users["laila"], products["Framed Abstract Wall Art"]);

            AddWishlistItem(context, users["youssef"], products["Weatherproof Lounge Chair"]);
        }

        private static void AddWishlistItem(
            AppDbContext context,
            AppUser user,
            Product product,
            decimal? originalPrice = null)
        {
            context.WishlistItems.Add(new WishlistItem
            {
                User = user,
                Product = product,
                ProductOriginalPrice = originalPrice ?? product.Price
            });
        }

        private static Dictionary<string, Order> AddOrders(
            AppDbContext context,
            Dictionary<string, AppUser> users,
            Dictionary<string, Product> products)
        {
            var now = DateTime.UtcNow;
            var orders = new Dictionary<string, Order>();

            var mariamDeliveredOld = CreateOrder(context, users["mariam"], OrderStatus.Delivered,
                now.AddDays(-35), 75m,
                [(products["Nordic 3-Seater Linen Sofa"], 1), (products["Terracotta Vase Trio"], 2)],
                deliveredAt: now.AddDays(-33));
            AddPaymentTransaction(context, mariamDeliveredOld, 910000001, 810000001, "Success",
                now.AddDays(-35), isSettled: true, merchantCommission: 243.68m);
            orders["mariam-delivered-old"] = mariamDeliveredOld;

            var mariamDeliveredRecent = CreateOrder(context, users["mariam"], OrderStatus.Delivered,
                now.AddDays(-5), 75m,
                [(products["Oslo Velvet Accent Chair"], 2), (products["Handwoven Wool Area Rug"], 1)],
                couponRate: 10m, deliveredAt: now.AddDays(-5));
            AddPaymentTransaction(context, mariamDeliveredRecent, 910000002, 810000002, "Success",
                now.AddDays(-5), isSettled: true, merchantCommission: 173.21m);
            orders["mariam-delivered-recent"] = mariamDeliveredRecent;

            var mariamProcessing = CreateOrder(context, users["mariam"], OrderStatus.Processing,
                now.AddDays(-2), 75m,
                [(products["Marble Top Coffee Table"], 1), (products["Ceramic Table Lamp Set"], 1)]);
            AddPaymentTransaction(context, mariamProcessing, 910000003, 810000003, "Success",
                now.AddDays(-2), isSettled: false, merchantCommission: 108.23m);
            orders["mariam-processing"] = mariamProcessing;

            var omarShipped = CreateOrder(context, users["omar"], OrderStatus.Shipped,
                now.AddDays(-3), 100m,
                [(products["Oakridge 6-Seater Dining Table"], 1), (products["Pendant Cluster Light"], 1)]);
            AddPaymentTransaction(context, omarShipped, 910000004, 810000004, "Success",
                now.AddDays(-3), isSettled: true, merchantCommission: 222.30m);
            orders["omar-shipped"] = omarShipped;

            var omarDelivered = CreateOrder(context, users["omar"], OrderStatus.Delivered,
                now.AddDays(-11), 100m,
                [(products["Aurora Upholstered King Bed"], 1), (products["Cedar 6-Door Wardrobe"], 1)],
                deliveredAt: now.AddDays(-9), paymentStatus: "Success");
            AddPaymentTransaction(context, omarDelivered, 910000005, 810000005, "Success",
                now.AddDays(-11), isSettled: true, merchantCommission: 441m, refundedAmount: 21_600m);
            orders["omar-delivered"] = omarDelivered;

            var lailaCancellationRequested = CreateOrder(context, users["laila"], OrderStatus.CancellationRequested,
                now.AddDays(-1), 75m,
                [(products["Milano Leather Corner Sofa"], 1)]);
            AddPaymentTransaction(context, lailaCancellationRequested, 910000006, 810000006, "Success",
                now.AddDays(-1), isSettled: false, merchantCommission: 350.78m);
            orders["laila-cancellation-requested"] = lailaCancellationRequested;

            var youssefCancelled = CreateOrder(context, users["youssef"], OrderStatus.Cancelled,
                now.AddDays(-12), 120m,
                [(products["Cloud Compact 2-Seater Sofa"], 1)],
                paymentStatus: "Voided", updatedAt: now.AddDays(-11));
            AddPaymentTransaction(context, youssefCancelled, 910000007, 810000007, "Voided",
                now.AddDays(-12), isSettled: false, merchantCommission: 151.83m);
            orders["youssef-cancelled"] = youssefCancelled;

            var lailaPending = CreateOrder(context, users["laila"], OrderStatus.Pending,
                now.AddHours(-3), 120m,
                [(products["Terracotta Vase Trio"], 1), (products["Framed Abstract Wall Art"], 1)],
                paymentMethod: "Cash", paymentStatus: "Pending");
            orders["laila-pending"] = lailaPending;

            var omarConfirmed = CreateOrder(context, users["omar"], OrderStatus.Confirmed,
                now.AddDays(-1), 100m,
                [(products["Executive Recliner Armchair"], 1)]);
            AddPaymentTransaction(context, omarConfirmed, 910000008, 810000008, "Success",
                now.AddDays(-1), isSettled: true, merchantCommission: 111.60m);
            orders["omar-confirmed"] = omarConfirmed;

            var mariamOutForDelivery = CreateOrder(context, users["mariam"], OrderStatus.OutForDelivery,
                now.AddDays(-2), 75m,
                [(products["Bedside Drawer Unit"], 2)],
                paymentMethod: "Cash", paymentStatus: "Pending");
            orders["mariam-out-for-delivery"] = mariamOutForDelivery;

            return orders;
        }

        private static Order CreateOrder(
            AppDbContext context,
            AppUser customer,
            OrderStatus status,
            DateTime createdAt,
            decimal shippingFee,
            (Product Product, int Quantity)[] lines,
            decimal couponRate = 0m,
            DateTime? deliveredAt = null,
            string paymentMethod = "Visa",
            string paymentStatus = "Completed",
            DateTime? updatedAt = null)
        {
            var order = new Order
            {
                Customer = customer,
                OrderStatus = status,
                ShippingAddress = customer.Address,
                ShippingFee = shippingFee,
                CouponDiscount = couponRate,
                UpdatedAt = updatedAt ?? deliveredAt ?? createdAt,
                ReviewReminderSentAt = status == OrderStatus.Delivered ? createdAt.AddDays(2) : null
            };

            decimal subtotal = 0m;
            var orderItems = new List<OrderItem>();
            foreach (var (product, quantity) in lines)
            {
                decimal unitPrice = couponRate == 0m
                    ? product.Price
                    : Math.Round(product.Price - (product.Price * couponRate / 100m), 2, MidpointRounding.AwayFromZero);

                orderItems.Add(new OrderItem
                {
                    Product = product,
                    Quantity = quantity,
                    UnitPrice = unitPrice
                });
                subtotal += unitPrice * quantity;
            }

            order.TotalPrice = subtotal + shippingFee;
            order.OrderItems = orderItems;
            context.Orders.Add(order);

            SetCreatedAt(context, order, createdAt);
            foreach (var item in orderItems)
                SetCreatedAt(context, item, createdAt);

            var payment = new Payment
            {
                Order = order,
                PaymentMethod = paymentMethod,
                Amount = (double)order.TotalPrice,
                PaymentStatus = paymentStatus
            };
            context.Payments.Add(payment);
            SetCreatedAt(context, payment, createdAt);

            return order;
        }

        private static void AddPaymentTransaction(
            AppDbContext context,
            Order order,
            long paymobTransactionId,
            long paymobOrderId,
            string status,
            DateTime createdAt,
            bool isSettled,
            decimal merchantCommission,
            decimal? refundedAmount = null)
        {
            var rawPayload = JsonSerializer.Serialize(new
            {
                id = paymobTransactionId,
                success = status == "Success",
                pending = false,
                is_refunded = false,
                is_voided = status == "Voided",
                amount_cents = (long)Math.Round(order.TotalPrice * 100m),
                currency = "EGP",
                order = new { id = paymobOrderId, merchant_order_id = order.OrderId.ToString() },
                source_data = new { type = "card", subtype = "card" }
            });

            var transaction = new PaymentTransaction
            {
                Order = order,
                PaymobTransactionId = paymobTransactionId,
                PaymobOrderId = paymobOrderId,
                Amount = order.TotalPrice,
                Currency = "EGP",
                Status = status,
                PaymentMethod = "card",
                SourceType = "card",
                Is3DSecure = true,
                ResponseCode = status == "Success" ? "APPROVED" : null,
                RefundedAmount = refundedAmount,
                IsSettled = isSettled,
                MerchantCommission = merchantCommission,
                PaymobCreatedAt = createdAt,
                RawPayload = rawPayload
            };

            context.PaymentTransactions.Add(transaction);
            SetCreatedAt(context, transaction, createdAt);
        }

        private static void AddCancellationRequests(
            AppDbContext context,
            Dictionary<string, AppUser> users,
            Dictionary<string, Order> orders)
        {
            var now = DateTime.UtcNow;

            var pending = new CancellationRequest
            {
                Order = orders["laila-cancellation-requested"],
                Customer = users["laila"],
                Reason = "I found the same sofa at a lower price nearby.",
                PreviousOrderStatus = nameof(OrderStatus.Processing),
                Status = CancellationRequestStatus.Pending,
                UpdatedAt = now.AddDays(-1)
            };
            context.CancellationRequests.Add(pending);
            SetCreatedAt(context, pending, now.AddDays(-1));

            var approved = new CancellationRequest
            {
                Order = orders["youssef-cancelled"],
                Customer = users["youssef"],
                Reason = "Ordered the wrong colour by mistake.",
                PreviousOrderStatus = nameof(OrderStatus.Processing),
                Status = CancellationRequestStatus.Approved,
                AdminNote = "Approved before dispatch; the payment hold was released.",
                ProcessedAt = now.AddDays(-11),
                UpdatedAt = now.AddDays(-11)
            };
            context.CancellationRequests.Add(approved);
            SetCreatedAt(context, approved, now.AddDays(-12));
        }

        private static void AddReturnRequests(
            AppDbContext context,
            Dictionary<string, AppUser> users,
            Dictionary<string, Order> orders)
        {
            var now = DateTime.UtcNow;

            var recentOrder = orders["mariam-delivered-recent"];
            var osloItem = recentOrder.OrderItems.First(oi => oi.Product.ProductName == "Oslo Velvet Accent Chair");
            var pending = new ReturnRequest
            {
                Order = recentOrder,
                Customer = users["mariam"],
                ReasonCategory = ReturnReasonCategory.CustomerChoice,
                ReasonText = "The colour is a little warmer than the website photos. I would like to return one chair.",
                Status = ReturnRequestStatus.Pending,
                UpdatedAt = now.AddDays(-4),
                ReturnItems = new List<ReturnItem>
                {
                    new() { OrderItem = osloItem, ProductName = "Oslo Velvet Accent Chair", Quantity = 1 }
                }
            };
            context.ReturnRequests.Add(pending);
            SetCreatedAt(context, pending, now.AddDays(-5));
            foreach (var item in pending.ReturnItems)
                SetCreatedAt(context, item, now.AddDays(-5));

            var deliveredOrder = orders["omar-delivered"];
            var wardrobeItem = deliveredOrder.OrderItems.First(oi => oi.Product.ProductName == "Cedar 6-Door Wardrobe");
            var completed = new ReturnRequest
            {
                Order = deliveredOrder,
                Customer = users["omar"],
                ReasonCategory = ReturnReasonCategory.MerchantDefect,
                ReasonText = "Two of the wardrobe doors arrived with deep scratches.",
                Status = ReturnRequestStatus.Completed,
                CourierName = "Bosta",
                CourierPhone = "+201001234567",
                ReceivedAt = now.AddDays(-7),
                RefundAmountCents = 2_160_000,
                AdminNote = "Refund approved after inspecting the damaged doors; shipping included.",
                ProcessedAt = now.AddDays(-6),
                UpdatedAt = now.AddDays(-6),
                ReturnItems = new List<ReturnItem>
                {
                    new() { OrderItem = wardrobeItem, ProductName = "Cedar 6-Door Wardrobe", Quantity = 1 }
                }
            };
            context.ReturnRequests.Add(completed);
            SetCreatedAt(context, completed, now.AddDays(-8));
            foreach (var item in completed.ReturnItems)
                SetCreatedAt(context, item, now.AddDays(-8));
        }

        private static void AddReviews(
            AppDbContext context,
            Dictionary<string, AppUser> users,
            Dictionary<string, Product> products)
        {
            var now = DateTime.UtcNow;

            AddReview(context, users["mariam"], products["Nordic 3-Seater Linen Sofa"], 5,
                "Beautiful craftsmanship and the linen feels premium. Delivery was smooth.", now.AddDays(-33));
            AddReview(context, users["mariam"], products["Handwoven Wool Area Rug"], 4,
                "Lovely texture and colour; it sheds a little at first.", now.AddDays(-3));
            AddReview(context, users["mariam"], products["Oslo Velvet Accent Chair"], 4,
                "Comfortable and sturdy. The colour is a touch warmer than the photo.", now.AddDays(-4));
            AddReview(context, users["omar"], products["Aurora Upholstered King Bed"], 5,
                "Assembly took an hour but the bed is rock solid.", now.AddDays(-7));
            AddReview(context, users["omar"], products["Cedar 6-Door Wardrobe"], 2,
                "Two doors arrived scratched, though support arranged a quick refund.", now.AddDays(-8));
        }

        private static void AddReview(
            AppDbContext context,
            AppUser user,
            Product product,
            int rating,
            string comment,
            DateTime createdAt)
        {
            var review = new Review
            {
                Customer = user,
                Product = product,
                Rating = rating,
                Comment = comment
            };
            context.Reviews.Add(review);
            SetCreatedAt(context, review, createdAt);
        }

        private static void AddSupportTickets(AppDbContext context, Dictionary<string, AppUser> users)
        {
            var now = DateTime.UtcNow;
            var mariam = users["mariam"];
            var omar = users["omar"];
            var laila = users["laila"];
            var youssef = users["youssef"];
            var nour = users["agent1"];
            var karim = users["agent2"];

            AddTicket(context, mariam, "Sofa cover arrived with a stain",
                SupportTicketStatus.Resolved, SupportTicketPriority.High,
                now.AddDays(-6), now.AddDays(-3), SupportConversationStatus.Closed,
                (mariam, "Hi, the cover on my Nordic sofa has a small oil stain near the armrest. Can you help?", now.AddDays(-6), true),
                (nour, "Hi Mariam, sorry about that! Could you send a photo of the stain?", now.AddDays(-6), true),
                (mariam, "Sent it through the chat. It is about 3 cm wide.", now.AddDays(-5), true),
                (nour, "Thanks for the photo. We have arranged a replacement cover free of charge; it should arrive within 3 days.", now.AddDays(-4), true),
                (mariam, "The new cover arrived today. Thank you for the quick help!", now.AddDays(-3), true));

            AddTicket(context, omar, "Can I change my delivery address?",
                SupportTicketStatus.InProgress, SupportTicketPriority.Medium,
                now.AddDays(-2), null, SupportConversationStatus.Active,
                (omar, "My order is marked as shipped but I need it delivered to my office instead. Is that possible?", now.AddDays(-2), true),
                (karim, "Hi Omar, we have contacted the courier. Please share the new address here and we will try to update it.", now.AddDays(-1), true),
                (omar, "It is 22 Sidi Gaber Street, Smouha. Thank you!", now.AddDays(-1), false));

            AddTicket(context, laila, "Question about cancellation timeline",
                SupportTicketStatus.Open, SupportTicketPriority.Low,
                now.AddDays(-1), null, SupportConversationStatus.Active,
                (laila, "I requested a cancellation yesterday for my sofa order. How long does the review usually take?", now.AddDays(-1), false));

            AddTicket(context, youssef, "Assembly instructions missing",
                SupportTicketStatus.WaitingForCustomer, SupportTicketPriority.Medium,
                now.AddDays(-4), null, SupportConversationStatus.Active,
                (youssef, "The wall art arrived without the hanging kit and instructions.", now.AddDays(-4), true),
                (nour, "Sorry about that! Could you confirm the model number printed on the box?", now.AddDays(-3), true));

            AddTicket(context, omar, "Do you offer gift wrapping?",
                SupportTicketStatus.Closed, SupportTicketPriority.Low,
                now.AddDays(-10), now.AddDays(-9), SupportConversationStatus.Closed,
                (omar, "Do you offer gift wrapping for the pendant light?", now.AddDays(-10), true),
                (karim, "Yes! Add a note at checkout and we will wrap it free of charge.", now.AddDays(-9), true));
        }

        private static void AddTicket(
            AppDbContext context,
            AppUser customer,
            string subject,
            SupportTicketStatus status,
            SupportTicketPriority priority,
            DateTime createdAt,
            DateTime? closedAt,
            SupportConversationStatus conversationStatus,
            params (AppUser Sender, string Content, DateTime CreatedAt, bool IsRead)[] messages)
        {
            var ticket = new SupportTicket
            {
                Customer = customer,
                Subject = subject,
                Status = status,
                Priority = priority,
                ClosedAt = closedAt,
                UpdatedAt = closedAt ?? createdAt
            };

            var conversation = new SupportConversation
            {
                SupportTicket = ticket,
                Status = conversationStatus,
                ClosedAt = closedAt,
                UpdatedAt = closedAt ?? createdAt,
                Messages = messages.Select(message => new SupportMessage
                {
                    Sender = message.Sender,
                    Content = message.Content,
                    IsRead = message.IsRead,
                    ReadAt = message.IsRead ? message.CreatedAt.AddHours(1) : null
                }).ToList()
            };

            context.SupportTickets.Add(ticket);
            context.SupportConversations.Add(conversation);
            SetCreatedAt(context, ticket, createdAt);
            SetCreatedAt(context, conversation, createdAt);

            for (int i = 0; i < messages.Length; i++)
                SetCreatedAt(context, conversation.Messages.ElementAt(i), messages[i].CreatedAt);
        }

        private static void AddNotifications(
            AppDbContext context,
            Dictionary<string, AppUser> users,
            Dictionary<string, Order> orders)
        {
            var now = DateTime.UtcNow;
            var admin = users["admin"];
            var mariam = users["mariam"];
            var omar = users["omar"];
            var laila = users["laila"];
            var youssef = users["youssef"];

            AddNotification(context, admin, mariam,
                "Your order has been delivered. We would love to hear your feedback.",
                $"api/orders/{orders["mariam-delivered-old"].OrderId}/details", now.AddDays(-33), true);

            AddNotification(context, admin, mariam,
                "Your return request for the Oslo Velvet Accent Chair is under review.",
                $"api/orders/{orders["mariam-delivered-recent"].OrderId}/details", now.AddDays(-4), true);

            AddNotification(context, admin, omar,
                "Your order has been shipped and is on its way.",
                $"api/orders/{orders["omar-shipped"].OrderId}/details", now.AddDays(-3), false);

            AddNotification(context, admin, omar,
                "Your refund of 21,600.00 EGP for the damaged wardrobe has been processed.",
                $"api/orders/{orders["omar-delivered"].OrderId}/details", now.AddDays(-6), false);

            AddNotification(context, users["agent1"], laila,
                "We received your cancellation request and will review it within 24 hours.",
                $"api/orders/{orders["laila-cancellation-requested"].OrderId}/details", now.AddDays(-1), false);

            AddNotification(context, admin, youssef,
                "Your order was cancelled and the payment hold released.",
                $"api/orders/{orders["youssef-cancelled"].OrderId}/details", now.AddDays(-11), true);

            AddNotification(context, admin, omar,
                "Your order is confirmed. We are preparing your items for dispatch.",
                $"api/orders/{orders["omar-confirmed"].OrderId}/details", now.AddDays(-1), false);

            AddNotification(context, users["agent1"], mariam,
                "A replacement sofa cover has been arranged and will ship within 3 days.",
                $"api/orders/{orders["mariam-delivered-old"].OrderId}/details", now.AddDays(-4), true);
        }

        private static void AddNotification(
            AppDbContext context,
            AppUser sender,
            AppUser receiver,
            string message,
            string link,
            DateTime createdAt,
            bool isRead)
        {
            var notification = new Notification
            {
                Sender = sender,
                Receiver = receiver,
                Message = message,
                Link = link,
                IsRead = isRead
            };
            context.Notifications.Add(notification);
            SetCreatedAt(context, notification, createdAt);
        }

        private static void AddIdempotencyRecords(
            AppDbContext context,
            Dictionary<string, AppUser> users,
            Dictionary<string, Order> orders)
        {
            var now = DateTime.UtcNow;
            AddIdempotencyRecord(context, users["mariam"], orders["mariam-processing"],
                "checkout-demo-0001", now.AddDays(-2));
            AddIdempotencyRecord(context, users["omar"], orders["omar-shipped"],
                "checkout-demo-0002", now.AddDays(-3));
        }

        private static void AddIdempotencyRecord(
            AppDbContext context,
            AppUser user,
            Order order,
            string key,
            DateTime createdAt)
        {
            var record = new IdempotencyRecord
            {
                User = user,
                Order = order,
                IdempotencyKey = key,
                ResponsePayload = JsonSerializer.Serialize(new { orderId = order.OrderId })
            };
            context.IdempotencyRecords.Add(record);
            SetCreatedAt(context, record, createdAt);
        }

        // CreatedAt has a private setter, so demo rows are backdated through the change tracker.
        private static void SetCreatedAt(AppDbContext context, object entity, DateTime createdAt)
        {
            context.Entry(entity).Property("CreatedAt").CurrentValue = createdAt;
        }
    }
}
