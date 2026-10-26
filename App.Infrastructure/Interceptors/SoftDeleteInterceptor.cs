using App.Domain.Interceptors;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace App.Infrastructure.Interceptors
{
    public class SoftDeleteInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context is null)
                return base.SavingChangesAsync(eventData, result, cancellationToken);

            var entries = eventData.Context.ChangeTracker
                .Entries<ISoftDeletable>()
                .Where(e => e.State == Microsoft.EntityFrameworkCore.EntityState.Deleted);

            foreach (var entry in entries)
            {
                entry.State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAt = DateTime.UtcNow;
            }
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}
