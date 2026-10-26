using App.Application.Common.Interfaces.Services;
using StackExchange.Redis;
using System.Text.Json;

namespace App.Infrastructure.Services
{
    public class MemoryService<T>(IConnectionMultiplexer redis) : IMemoryService<T>
    {
        private readonly IDatabase _database = redis.GetDatabase();

        public async Task<T> GetObjectAsync(string key)
        {
            var jsonRes = await _database.StringGetAsync(key);
            if (jsonRes.IsNullOrEmpty) return default!;
            T res = JsonSerializer.Deserialize<T>(jsonRes!)!;
            return res;
        }
        public async Task<bool> SetObjectAsync(string key, T value)
        {
            var json = JsonSerializer.Serialize(value);
            return await _database.StringSetAsync(key, json, TimeSpan.FromHours(1));
        }
        public async Task<bool> SetObjectAsync(string key, T value, TimeSpan absoluteExpirationRelativeToNow)
        {
            var json = JsonSerializer.Serialize(value);
            return await _database.StringSetAsync(key, json, absoluteExpirationRelativeToNow);
        }
        public async Task<bool> RemoveObjectAsync(string key)
        {
            return await _database.KeyDeleteAsync(key);
        }
    }

}