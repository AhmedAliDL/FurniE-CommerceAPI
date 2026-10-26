using App.Application.Common.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace App.Infrastructure.Services
{
    public class RecentlyViewedService(IConnectionMultiplexer redis, IConfiguration configuration) : IRecentlyViewedService
    {
        private readonly IDatabase _database = redis.GetDatabase();

        private int MaxItems => configuration.GetValue("RecentlyViewed:MaxItems", 10);
        private TimeSpan Expiry => TimeSpan.FromDays(configuration.GetValue("RecentlyViewed:ExpiryDays", 30));

        public async Task RecordViewAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default)
        {
            var key = Key(userId);
            var productIdValue = productId.ToString();

            await _database.ListRemoveAsync(key, productIdValue);
            await _database.ListLeftPushAsync(key, productIdValue);
            await _database.ListTrimAsync(key, 0, MaxItems - 1);
            await _database.KeyExpireAsync(key, Expiry);
        }

        public async Task<List<Guid>> GetRecentProductIdsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var values = await _database.ListRangeAsync(Key(userId), 0, MaxItems - 1);
            return values.Select(v => Guid.Parse(v.ToString())).ToList();
        }

        private static string Key(Guid userId) => $"recently-viewed:{userId}";
    }
}
