using App.Application.Common.Interfaces.Services;
using App.Domain.Entities;
using App.Domain.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text.Json;

namespace App.Infrastructure.Interceptors
{
    public class AuditInterceptor(ICurrentUserService currentUserService) : SaveChangesInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context is null)
                return await base.SavingChangesAsync(eventData, result, cancellationToken);

            var entries = eventData.Context.ChangeTracker
                .Entries<IAuditLog>()
                .Where(e =>
                e.State == EntityState.Added ||
                e.State == EntityState.Modified ||
                e.State == EntityState.Deleted
                ).ToList();

            foreach (var entry in entries)
            {
                var oldValues = entry.OriginalValues.Properties.ToDictionary(p => p.Name, p => entry.OriginalValues[p]);
                var newValues = entry.CurrentValues.Properties.ToDictionary(p => p.Name, p => entry.CurrentValues[p]);
                AuditLog audit = new()
                {
                    UserId = currentUserService.UserId,
                    Action = entry.State.ToString(),
                    EntityName = entry.Entity.ToString()!,
                    EntityId = Guid.Parse(entry.Property("Id").CurrentValue!.ToString()!),
                    OldValues = JsonSerializer.Serialize(oldValues),
                    NewValues = JsonSerializer.Serialize(newValues)
                };
                await eventData.Context.Set<AuditLog>().AddAsync(audit, cancellationToken);
            }
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}
