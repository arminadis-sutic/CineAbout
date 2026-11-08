using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;
using CineAboutApp.Services.Database;
using Microsoft.EntityFrameworkCore;

namespace CineAboutApp.Services.Controller
{
    public class AuditLogService(ApplicationDbContext db)
    {
        public async Task<MyPagedList<AuditLogResponse>> GetAsync(AuditLogSearchObject search, CancellationToken cancellationToken = default)
        {
            var query = db.AuditLogs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search.EntityType))
            {
                query = query.Where(x => x.EntityType == search.EntityType);
            }

            if (!string.IsNullOrWhiteSpace(search.Action))
            {
                query = query.Where(x => x.Action == search.Action);
            }

            if (search.PerformedByUserId.HasValue)
            {
                query = query.Where(x => x.PerformedByUserId == search.PerformedByUserId.Value);
            }

            if (search.From.HasValue)
            {
                query = query.Where(x => x.PerformedAt >= search.From.Value);
            }

            if (search.To.HasValue)
            {
                query = query.Where(x => x.PerformedAt <= search.To.Value);
            }

            var pageSize = Math.Clamp(search.PageSize.GetValueOrDefault(10), 1, 100);
            var pageNumber = Math.Max(search.PageNumber.GetValueOrDefault(1), 1);
            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderByDescending(x => x.PerformedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new AuditLogResponse
                {
                    Id = x.Id,
                    EntityType = x.EntityType,
                    EntityId = x.EntityId,
                    Action = x.Action,
                    OldValuesJson = x.OldValuesJson,
                    NewValuesJson = x.NewValuesJson,
                    PerformedByUserId = x.PerformedByUserId,
                    PerformedByUsername = x.PerformedByUsername,
                    IpAddress = x.IpAddress,
                    UserAgent = x.UserAgent,
                    PerformedAt = x.PerformedAt
                })
                .ToListAsync(cancellationToken);

            return new MyPagedList<AuditLogResponse>(items, totalCount, pageNumber, pageSize);
        }
    }
}



