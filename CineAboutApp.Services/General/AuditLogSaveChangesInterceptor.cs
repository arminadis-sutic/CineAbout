using System.Text.Json;
using CineAboutApp.Services.Database;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CineAboutApp.Services.General
{
    public class AuditLogSaveChangesInterceptor(IHttpContextAccessor httpContextAccessor) : SaveChangesInterceptor
    {
        private static readonly HashSet<Type> AuditedTypes = new()
        {
            typeof(Movie),
            typeof(Review),
            typeof(CineAboutApp.Services.Database.Thread),
            typeof(Comment),
            typeof(MyAppUser),
            typeof(WatchlistItem),
            typeof(QuizSession),
            typeof(Follow)
        };

        private readonly List<(AuditLog Log, EntityEntry Entry)> _added = new();

        public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
        {
            if (_added.Count > 0 && eventData.Context != null)
            {
                FillRealIds();
                eventData.Context.SaveChanges();
            }
            return base.SavedChanges(eventData, result);
        }

        public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            if (_added.Count > 0 && eventData.Context != null)
            {
                FillRealIds();
                await eventData.Context.SaveChangesAsync(cancellationToken);
            }
            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        public override void SaveChangesFailed(DbContextErrorEventData eventData) => _added.Clear();

        public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            _added.Clear();
            return Task.CompletedTask;
        }

        private void FillRealIds()
        {
            var pending = _added.ToList();
            _added.Clear();
            foreach (var (log, entry) in pending)
            {
                log.EntityId = GetPrimaryKey(entry);
            }
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            var context = eventData.Context;
            if (context == null)
                return base.SavingChangesAsync(eventData, result, cancellationToken);

            var entries = context.ChangeTracker.Entries()
                .Where(e => e.Entity != null)
                .Where(IsAuditedEntry)
                .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                .ToList();

            if (entries.Count == 0)
                return base.SavingChangesAsync(eventData, result, cancellationToken);

            var logs = new List<AuditLog>();
            foreach (var entry in entries)
            {
                var action = GetAction(entry);
                if (string.IsNullOrWhiteSpace(action))
                    continue;

                var log = new AuditLog
                {
                    EntityType = entry.Entity.GetType().Name,
                    EntityId = GetPrimaryKey(entry),
                    Action = action,
                    OldValuesJson = action == "Create" ? null : SerializeValues(entry, useOriginalValues: true),
                    NewValuesJson = action == "Delete" ? null : SerializeValues(entry, useOriginalValues: false),
                    PerformedByUserId = GetUserId(),
                    PerformedByUsername = GetUsername(),
                    IpAddress = GetIpAddress(),
                    UserAgent = GetUserAgent(),
                    PerformedAt = DateTime.UtcNow
                };

                logs.Add(log);
                if (entry.State == EntityState.Added)
                {
                    _added.Add((log, entry));
                }
            }

            if (logs.Count > 0)
            {
                context.Set<AuditLog>().AddRange(logs);
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private static bool IsAuditedEntry(EntityEntry entry)
        {
            var type = entry.Entity.GetType();
            return AuditedTypes.Contains(type) && type != typeof(AuditLog);
        }

        private static string GetPrimaryKey(EntityEntry entry)
        {
            var keyProps = entry.Properties.Where(p => p.Metadata.IsPrimaryKey()).ToList();
            if (keyProps.Count == 0)
                return string.Empty;
            return string.Join(",", keyProps.Select(p => p.CurrentValue?.ToString() ?? string.Empty));
        }

        private static string? SerializeValues(EntityEntry entry, bool useOriginalValues)
        {
            var values = new Dictionary<string, object?>();

            foreach (var prop in entry.Properties)
            {
                if (prop.Metadata.IsPrimaryKey())
                    continue;

                if (entry.State == EntityState.Modified && !prop.IsModified && !IsSoftDeleteTransition(entry, prop))
                    continue;

                values[prop.Metadata.Name] = useOriginalValues ? prop.OriginalValue : prop.CurrentValue;
            }

            return values.Count == 0 ? null : JsonSerializer.Serialize(values);
        }

        private static string GetAction(EntityEntry entry)
        {
            if (entry.State == EntityState.Added)
                return "Create";
            if (entry.State == EntityState.Deleted)
                return "Delete";
            if (entry.State == EntityState.Modified)
            {
                var softDelete = entry.Properties.Any(p => p.Metadata.Name == "IsDeleted" && p.IsModified && p.CurrentValue is bool b && b);
                return softDelete ? "Delete" : "Update";
            }
            return string.Empty;
        }

        private static bool IsSoftDeleteTransition(EntityEntry entry, PropertyEntry prop)
        {
            return entry.State == EntityState.Modified
                && prop.Metadata.Name == "IsDeleted"
                && prop.IsModified;
        }

        private int? GetUserId()
        {
            var userIdValue = httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;
            return int.TryParse(userIdValue, out var userId) ? userId : null;
        }

        private string? GetUsername()
        {
            var user = httpContextAccessor.HttpContext?.User;
            if (user == null)
                return null;
            return user.FindFirst("Username")?.Value ?? user.Identity?.Name;
        }

        private string? GetIpAddress()
        {
            return httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
        }

        private string? GetUserAgent()
        {
            return httpContextAccessor.HttpContext?.Request?.Headers.UserAgent.ToString();
        }
    }
}

