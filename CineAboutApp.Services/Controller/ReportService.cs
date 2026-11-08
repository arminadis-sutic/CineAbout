using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Interfaces.General;
using CineAboutApp.Model;
using CineAboutApp.Models.Enums;
using CineAboutApp.Models.Requests.Report;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;
using CineAboutApp.Services.Database;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace CineAboutApp.Services.Controller
{
    public class ReportService(
        ApplicationDbContext db,
        IMapper mapper,
        IIdentityService identity,
        IUserNotifier notifier)
        : BaseCRUDService<ReportResponse, ReportSearchObject, ReportInsertOrUpdateRequest, ReportInsertOrUpdateRequest, Report>(db, mapper), IReportService
    {
        private static readonly Dictionary<ReportStatus, ReportStatus[]> AllowedTransitions = new()
        {
            [ReportStatus.Pending] = [ReportStatus.Resolved, ReportStatus.Dismissed],
            [ReportStatus.Resolved] = [],
            [ReportStatus.Dismissed] = []
        };

        private sealed record Target(int AuthorId, string Preview);

        protected override IQueryable<Report> GetIncludeQuery(IQueryable<Report> query)
        {
            return query.Include(r => r.Reporter).Include(r => r.ReviewedByUser);
        }

        protected override IQueryable<Report> ApplyFilters(IQueryable<Report> query, ReportSearchObject search)
        {
            if (search.TargetType.HasValue)
            {
                query = query.Where(x => x.TargetType == search.TargetType.Value);
            }

            if (search.Status.HasValue)
            {
                query = query.Where(x => x.Status == search.Status.Value);
            }

            if (search.TargetId.HasValue)
            {
                query = query.Where(x => x.TargetId == search.TargetId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search.NameFTS))
            {
                var term = search.NameFTS.Trim().ToLower();
                query = query.Where(x => x.Reason.ToLower().Contains(term) || x.Reporter.Username.ToLower().Contains(term));
            }

            return query;
        }

        protected override ReportResponse MapToResponse(Report entity) => new()
        {
            Id = entity.Id,
            ReporterId = entity.ReporterId,
            ReporterUsername = entity.Reporter?.Username ?? string.Empty,
            TargetType = entity.TargetType,
            TargetId = entity.TargetId,
            Reason = entity.Reason,
            Status = entity.Status,
            CreatedAt = entity.CreatedAt,
            ReviewedByUsername = entity.ReviewedByUser?.Username,
            ReviewedAt = entity.ReviewedAt,
            AdminNote = entity.AdminNote
        };

        public override async Task<MyPagedList<ReportResponse>> GetAsync(ReportSearchObject search, CancellationToken cancellation = default)
        {
            var page = await base.GetAsync(search, cancellation);
            await FillPreviewsAsync(page.DataItems, cancellation);
            return page;
        }

        public override async Task<ReportResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var report = await base.GetByIdAsync(id, cancellationToken);
            if (report != null)
                await FillPreviewsAsync([report], cancellationToken);
            return report;
        }

        public override async Task<ReportResponse> CreateAsync(ReportInsertOrUpdateRequest request, CancellationToken cancellationToken = default)
        {
            var reporterId = identity.GetCurrentUserId();
            var targets = await LoadTargetsAsync(request.TargetType, [request.TargetId], cancellationToken);
            if (!targets.TryGetValue(request.TargetId, out var target))
                throw new NotFoundException("Sadržaj koji prijavljujete", request.TargetId);

            if (target.AuthorId == reporterId)
                throw new BusinessException("Ne možete prijaviti vlastiti sadržaj.");

            if (await db.Reports.AnyAsync(r => r.ReporterId == reporterId
                    && r.TargetType == request.TargetType
                    && r.TargetId == request.TargetId
                    && r.Status == ReportStatus.Pending, cancellationToken))
            {
                throw new BusinessException("Već ste prijavili ovaj sadržaj. Prijava čeka obradu.");
            }

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            var entity = new Report
            {
                ReporterId = reporterId,
                TargetType = request.TargetType,
                TargetId = request.TargetId,
                Reason = request.Reason.Trim(),
                Status = ReportStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            db.Reports.Add(entity);

            if (request.TargetType == ReportTargetType.Review)
            {
                await db.Reviews
                    .Where(r => r.Id == request.TargetId && r.Status == ReviewStatus.Active)
                    .ExecuteUpdateAsync(s => s.SetProperty(r => r.Status, ReviewStatus.Reported), cancellationToken);
            }

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return (await GetByIdAsync(entity.Id, cancellationToken))!;
        }

        public async Task<ReportResponse> DecideAsync(int id, ReportDecisionRequest request, CancellationToken cancellationToken = default)
        {
            var report = await db.Reports.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
                ?? throw new NotFoundException("Prijava", id);

            if (!AllowedTransitions[report.Status].Contains(request.Status))
            {
                throw new BusinessException(report.Status == ReportStatus.Pending
                    ? "Prijava se može samo riješiti ili odbaciti."
                    : "Prijava je već obrađena i ne može se ponovo mijenjati.");
            }

            if (request.Status == ReportStatus.Dismissed && string.IsNullOrWhiteSpace(request.AdminNote))
                throw new BusinessException("Unesite razlog odbacivanja prijave.");

            var adminId = identity.GetCurrentUserId();
            var targets = await LoadTargetsAsync(report.TargetType, [report.TargetId], cancellationToken);
            targets.TryGetValue(report.TargetId, out var target);

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            var related = await db.Reports
                .Where(r => r.TargetType == report.TargetType && r.TargetId == report.TargetId && r.Status == ReportStatus.Pending)
                .ToListAsync(cancellationToken);

            var removeContent = request.Status == ReportStatus.Resolved && request.RemoveContent && target != null;
            foreach (var item in related)
            {
                item.Status = request.Status;
                item.ReviewedByUserId = adminId;
                item.ReviewedAt = DateTime.UtcNow;
                item.AdminNote = request.AdminNote?.Trim();
            }

            await db.SaveChangesAsync(cancellationToken);

            if (removeContent)
            {
                await RemoveTargetAsync(report.TargetType, report.TargetId, cancellationToken);
            }
            else if (request.Status == ReportStatus.Dismissed && report.TargetType == ReportTargetType.Review)
            {
                await db.Reviews
                    .Where(r => r.Id == report.TargetId && r.Status == ReviewStatus.Reported)
                    .ExecuteUpdateAsync(s => s.SetProperty(r => r.Status, ReviewStatus.Active), cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);

            var verdict = request.Status == ReportStatus.Resolved ? "riješena" : "odbačena";
            await notifier.NotifyAsync(
                related.Select(r => r.ReporterId),
                $"Prijava {verdict}",
                $"Vaša prijava je {verdict}. Hvala što činite zajednicu boljom.",
                new Dictionary<string, string> { ["type"] = "report_decision", ["reportId"] = id.ToString() },
                cancellationToken);

            if (removeContent && target != null)
            {
                await notifier.NotifyAsync(
                    [target.AuthorId],
                    "Sadržaj uklonjen",
                    "Vaš sadržaj je uklonjen nakon prijave zbog kršenja pravila zajednice.",
                    new Dictionary<string, string> { ["type"] = "content_removed" },
                    cancellationToken);
            }

            return (await GetByIdAsync(id, cancellationToken))!;
        }

        public override async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var report = await db.Reports.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
            if (report == null)
                return false;

            db.Reports.Remove(report);
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public override Task<ReportResponse?> UpdateAsync(int id, ReportInsertOrUpdateRequest request, CancellationToken cancellationToken = default)
            => throw new BusinessException("Prijavu nije moguće uređivati. Koristite odluku o prijavi.");

        private async Task RemoveTargetAsync(ReportTargetType type, int id, CancellationToken ct)
        {
            switch (type)
            {
                case ReportTargetType.Review:
                    await db.Reviews.Where(r => r.Id == id)
                        .ExecuteUpdateAsync(s => s.SetProperty(r => r.Status, ReviewStatus.Removed), ct);
                    break;
                case ReportTargetType.Thread:
                    await db.Threads.Where(t => t.Id == id)
                        .ExecuteUpdateAsync(s => s.SetProperty(t => t.Status, ThreadStatus.Removed), ct);
                    break;
                case ReportTargetType.Comment:
                    await db.CommentVotes.Where(v => v.CommentId == id).ExecuteDeleteAsync(ct);
                    await db.Comments.Where(c => c.Id == id).ExecuteDeleteAsync(ct);
                    break;
            }
        }

        private async Task<Dictionary<int, Target>> LoadTargetsAsync(ReportTargetType type, IReadOnlyCollection<int> ids, CancellationToken ct)
        {
            switch (type)
            {
                case ReportTargetType.Review:
                    return (await db.Reviews.AsNoTracking().Where(r => ids.Contains(r.Id))
                            .Select(r => new { r.Id, r.UserId, r.Text, r.Movie.Title }).ToListAsync(ct))
                        .ToDictionary(r => r.Id, r => new Target(r.UserId, Preview($"{r.Title}: {r.Text}")));
                case ReportTargetType.Thread:
                    return (await db.Threads.AsNoTracking().Where(t => ids.Contains(t.Id))
                            .Select(t => new { t.Id, t.UserId, t.Title }).ToListAsync(ct))
                        .ToDictionary(t => t.Id, t => new Target(t.UserId, Preview(t.Title)));
                default:
                    return (await db.Comments.AsNoTracking().Where(c => ids.Contains(c.Id))
                            .Select(c => new { c.Id, c.UserId, c.Content }).ToListAsync(ct))
                        .ToDictionary(c => c.Id, c => new Target(c.UserId, Preview(c.Content)));
            }
        }

        private async Task FillPreviewsAsync(List<ReportResponse> reports, CancellationToken ct)
        {
            foreach (var group in reports.GroupBy(r => r.TargetType))
            {
                var targets = await LoadTargetsAsync(group.Key, group.Select(r => r.TargetId).Distinct().ToList(), ct);
                foreach (var report in group)
                {
                    report.TargetPreview = targets.TryGetValue(report.TargetId, out var t) ? t.Preview : "[sadržaj je obrisan]";
                }
            }
        }

        private static string Preview(string text) => text.Length <= 120 ? text : text[..120] + "…";
    }
}
