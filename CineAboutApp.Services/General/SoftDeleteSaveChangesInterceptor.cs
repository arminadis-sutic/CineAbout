using CineAboutApp.Services.Database;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CineAboutApp.Services.General
{
    public class SoftDeleteSaveChangesInterceptor(IHttpContextAccessor httpContextAccessor) : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            var context = eventData.Context;
            if (context == null)
                return base.SavingChangesAsync(eventData, result, cancellationToken);

            foreach (var entry in context.ChangeTracker.Entries().Where(e => e.Entity is ISoftDeletable))
            {
                if (entry.State == EntityState.Deleted)
                {
                    SetSoftDelete(entry);
                }
                else if (entry.State == EntityState.Modified && entry.Property(nameof(ISoftDeletable.IsDeleted)).IsModified && (bool)entry.Property(nameof(ISoftDeletable.IsDeleted)).CurrentValue!)
                {
                    var softDelete = (ISoftDeletable)entry.Entity;
                    softDelete.DeletedAt ??= DateTime.UtcNow;
                    softDelete.DeletedByUserId ??= GetUserId();
                }
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private void SetSoftDelete(EntityEntry entry)
        {
            entry.State = EntityState.Modified;

            if (entry.Entity is ISoftDeletable softDelete)
            {
                softDelete.IsDeleted = true;
                softDelete.DeletedAt = DateTime.UtcNow;
                softDelete.DeletedByUserId = GetUserId();
            }
        }

        private int? GetUserId()
        {
            var userIdValue = httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;
            return int.TryParse(userIdValue, out var userId) ? userId : null;
        }
    }
}
