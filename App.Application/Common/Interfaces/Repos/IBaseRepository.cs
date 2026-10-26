using App.Application.Common.Enums;
using System.Linq.Expressions;

namespace App.Application.Common.Interfaces.Repos
{
    public interface IBaseRepository<T> where T : class
    {
        Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<T?> GetByIdAsync(Guid id);
        Task<T?> FindAsync(Expression<Func<T, bool>> criteria, CancellationToken cancellationToken = default);
        Task<T?> FindAsync(Expression<Func<T, bool>> criteria, Expression<Func<T, object>>[] includes, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<T>> FindAllAsync(Expression<Func<T, bool>> criteria, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<T>> FindAllAsync(Expression<Func<T, bool>> criteria, Expression<Func<T, object>>[] includes, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<T>> FindAllAsync(Expression<Func<T, bool>> criteria, SortDirection sortDirection, Expression<Func<T, object>> orderBy, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<T>> FindAllAsync(Expression<Func<T, bool>> criteria, Expression<Func<T, object>>[] includes, SortDirection sortDirection, Expression<Func<T, object>> orderBy, CancellationToken cancellationToken = default);
        Task<(IReadOnlyList<T> Items, int TotalCount)> FindPagedAsync(Expression<Func<T, bool>> criteria, Expression<Func<T, object>>[] includes, SortDirection sortDirection, Expression<Func<T, object>> orderBy, int page, int pageSize, CancellationToken cancellationToken = default);
        Task AddAsync(T entity, CancellationToken cancellationToken = default);
        Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
        void Update(T entity);
        void UpdateRange(IEnumerable<T> entities);
        void Delete(T entity);
        void DeleteRange(IEnumerable<T> entities);

    }
}
