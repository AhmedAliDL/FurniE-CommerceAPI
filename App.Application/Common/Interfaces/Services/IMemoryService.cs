namespace App.Application.Common.Interfaces.Services
{
    public interface IMemoryService<T>
    {
        Task<T> GetObjectAsync(string key);
        Task<bool> SetObjectAsync(string key, T value);
        Task<bool> SetObjectAsync(string key, T value, TimeSpan absoluteExpirationRelativeToNow);
        Task<bool> RemoveObjectAsync(string key);
    }
}