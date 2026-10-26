namespace App.Application.Common.Interfaces.Services
{
    public interface IRecentlyViewedService
    {
        Task RecordViewAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default);
        Task<List<Guid>> GetRecentProductIdsAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
