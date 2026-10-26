using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Infrastructure.BackgroundJobs;
using App.Infrastructure.Interceptors;
using App.Infrastructure.Services;
using CloudinaryDotNet;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace App.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var cloudinaryUrl = Environment.GetEnvironmentVariable("CLOUDINARY_URL")
                ?? configuration["Cloudinary:Url"]
                ?? throw new InvalidOperationException(
                    "Cloudinary is not configured. Set the CLOUDINARY_URL environment variable.");
            services.AddSingleton(_ => new Cloudinary(cloudinaryUrl) { Api = { Secure = true } });

            services.AddScoped<IIdentityService, IdentityService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped(typeof(IMemoryService<>), typeof(MemoryService<>));
            services.AddScoped<IJwtTokenService, JwtTokenService>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IContactService, ContactService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<ISupportService, SupportService>();
            services.AddScoped<IRecentlyViewedService, RecentlyViewedService>();
            services.AddSingleton<IShippingRateService, ShippingRateService>();
            services.AddSingleton<IEmailTemplateBuilder, EmailTemplateBuilder>();
            services.AddScoped<IFileStorageService, CloudinaryFileStorageService>();
            services.AddHttpClient();
            services.AddScoped<IPaymobService, PaymobService>();
            services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>();
            services.AddScoped<IJobs, CartExpirationBackgroundJob>();
            services.AddScoped<IJobs, OrderReviewReminderBackgroundJob>();
            services.AddScoped<IJobs, ScheduledOrderReminderBackgroundJob>();
            services.AddScoped<IPriceDropDetectionBackgroundJob, PriceDropDetectionBackgroundJob>();
            // Hangfire activates jobs by resolving their concrete type from DI.
            services.AddScoped<CartExpirationBackgroundJob>();
            services.AddScoped<OrderReviewReminderBackgroundJob>();
            services.AddScoped<ScheduledOrderReminderBackgroundJob>();
            services.AddScoped<PriceDropDetectionBackgroundJob>();
            services.AddScoped<SoftDeleteInterceptor>();
            services.AddScoped<AuditInterceptor>();
            services.AddSingleton<IConnectionMultiplexer>(_ =>
            {
                var connectionString =
            configuration.GetConnectionString("RedisConnection")
            ?? throw new InvalidOperationException(
                "Redis connection string is missing.");
                return ConnectionMultiplexer.Connect(connectionString);
            });
            services.AddHangfire(x => x.UseSqlServerStorage(configuration.GetConnectionString("DefaultConnection")));
            services.AddHangfireServer();
            services.AddSignalR();
            return services;
        }
    }
}