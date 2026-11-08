using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Interfaces.General;
using CineAboutApp.Model;
using CineAboutApp.Models.Enums;
using CineAboutApp.Models.Requests.Thread;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;
using CineAboutApp.Services.Database;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using ThreadEntity = CineAboutApp.Services.Database.Thread;

namespace CineAboutApp.Services.Controller
{
    public class ThreadService(
        ApplicationDbContext db,
        IMapper mapper,
        IIdentityService identity,
        IUserNotifier notifier)
        : BaseCRUDService<ThreadResponse, ThreadSearchObject, ThreadInsertOrUpdateRequest, ThreadInsertOrUpdateRequest, ThreadEntity>(db, mapper), IThreadService
    {
        private static readonly Dictionary<ThreadStatus, ThreadStatus[]> AllowedTransitions = new()
        {
            [ThreadStatus.Active] = [ThreadStatus.Locked, ThreadStatus.Removed],
            [ThreadStatus.Locked] = [ThreadStatus.Active, ThreadStatus.Removed],
            [ThreadStatus.Removed] = [ThreadStatus.Active]
        };

        protected override IQueryable<ThreadEntity> GetIncludeQuery(IQueryable<ThreadEntity> query)
        {
            return query
                .Include(t => t.User)
                .Include(t => t.Movie)
                .Include(t => t.ThreadTags).ThenInclude(tt => tt.Tag)
                .Include(t => t.ThreadVotes)
                .Include(t => t.Comments)
                .AsSplitQuery();
        }

        protected override IQueryable<ThreadEntity> ApplyFilters(IQueryable<ThreadEntity> query, ThreadSearchObject search)
        {
            if (!identity.IsAdmin())
            {
                query = query.Where(x => x.Status != ThreadStatus.Removed);
            }

            if (search.Status.HasValue)
            {
                query = query.Where(x => x.Status == search.Status.Value);
            }

            if (!string.IsNullOrWhiteSpace(search.NameFTS))
            {
                var term = search.NameFTS.Trim().ToLower();
                query = query.Where(x => x.Title.ToLower().Contains(term)
                    || x.Content.ToLower().Contains(term)
                    || x.ThreadTags.Any(tt => tt.Tag.Name.Contains(term)));
            }

            if (search.CreatedFrom.HasValue)
            {
                query = query.Where(x => x.CreatedAt >= search.CreatedFrom.Value.Date);
            }

            if (search.CreatedTo.HasValue)
            {
                var until = search.CreatedTo.Value.Date.AddDays(1);
                query = query.Where(x => x.CreatedAt < until);
            }

            if (search.MovieId.HasValue)
            {
                query = query.Where(x => x.MovieId == search.MovieId.Value);
            }

            if (search.UserId.HasValue)
            {
                query = query.Where(x => x.UserId == search.UserId.Value);
            }

            foreach (var tag in ParseTags(search.Tags))
            {
                var name = tag;
                query = query.Where(x => x.ThreadTags.Any(tt => tt.Tag.Name == name));
            }

            return query;
        }

        protected override IQueryable<ThreadEntity> ApplySorting(IQueryable<ThreadEntity> query, ThreadSearchObject search)
        {
            return search.SortBy == ThreadSortBy.Top
                ? query
                    .OrderByDescending(x => x.ThreadVotes.Count(v => v.IsUpvote) - x.ThreadVotes.Count(v => !v.IsUpvote))
                    .ThenByDescending(x => x.CreatedAt)
                : query.OrderByDescending(x => x.CreatedAt);
        }

        public override async Task<MyPagedList<ThreadResponse>> GetAsync(ThreadSearchObject search, CancellationToken cancellation = default)
        {
            var page = await base.GetAsync(search, cancellation);
            foreach (var item in page.DataItems)
                item.Comments = new List<CommentResponse>();
            return page;
        }

        public override async Task<ThreadResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await LoadDetailAsync(id, cancellationToken);
            if (entity == null || (entity.Status == ThreadStatus.Removed && !identity.IsAdmin()))
                return null;

            return MapToResponse(entity, includeComments: true);
        }

        protected override ThreadResponse MapToResponse(ThreadEntity entity) => MapToResponse(entity, includeComments: false);

        private ThreadResponse MapToResponse(ThreadEntity entity, bool includeComments)
        {
            var currentUserId = identity.GetCurrentUserIdNullable();
            var response = new ThreadResponse
            {
                Id = entity.Id,
                UserId = entity.UserId,
                Username = entity.User?.Username ?? string.Empty,
                Title = entity.Title,
                Content = entity.Content,
                HasSpoiler = entity.HasSpoiler,
                Status = entity.Status,
                CreatedAt = entity.CreatedAt,
                MovieId = entity.MovieId,
                MovieTitle = entity.Movie?.Title,
                Tags = entity.ThreadTags.Select(tt => tt.Tag.Name).OrderBy(n => n).ToList(),
                Score = entity.ThreadVotes.Count(v => v.IsUpvote) - entity.ThreadVotes.Count(v => !v.IsUpvote),
                CommentCount = entity.Comments.Count,
                MyVote = currentUserId == null
                    ? 0
                    : entity.ThreadVotes.Where(v => v.UserId == currentUserId).Select(v => v.IsUpvote ? 1 : -1).FirstOrDefault()
            };

            if (includeComments)
            {
                response.Comments = entity.Comments
                    .OrderBy(c => c.CreatedAt)
                    .Select(c => CommentService.ToResponse(c, currentUserId))
                    .ToList();
            }

            return response;
        }

        private async Task<ThreadEntity?> LoadDetailAsync(int id, CancellationToken ct)
        {
            return await db.Threads
                .AsNoTracking()
                .Include(t => t.User)
                .Include(t => t.Movie)
                .Include(t => t.ThreadTags).ThenInclude(tt => tt.Tag)
                .Include(t => t.ThreadVotes)
                .Include(t => t.Comments).ThenInclude(c => c.User)
                .Include(t => t.Comments).ThenInclude(c => c.CommentVotes)
                .AsSplitQuery()
                .FirstOrDefaultAsync(t => t.Id == id, ct);
        }

        public override async Task<ThreadResponse> CreateAsync(ThreadInsertOrUpdateRequest request, CancellationToken cancellationToken = default)
        {
            var userId = identity.GetCurrentUserId();
            await EnsureMovieExistsAsync(request.MovieId, cancellationToken);

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            var entity = new ThreadEntity
            {
                UserId = userId,
                Title = request.Title.Trim(),
                Content = request.Content.Trim(),
                HasSpoiler = request.HasSpoiler,
                MovieId = request.MovieId,
                Status = ThreadStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            db.Threads.Add(entity);
            await db.SaveChangesAsync(cancellationToken);

            await SetTagsAsync(entity.Id, request.Tags, cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return (await GetByIdAsync(entity.Id, cancellationToken))!;
        }

        public override async Task<ThreadResponse?> UpdateAsync(int id, ThreadInsertOrUpdateRequest request, CancellationToken cancellationToken = default)
        {
            var entity = await db.Threads.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
            if (entity == null)
                return null;

            EnsureOwnerOrAdmin(entity.UserId);

            if (!identity.IsAdmin() && entity.Status != ThreadStatus.Active)
                throw new BusinessException("Zaključanu diskusiju nije moguće uređivati.");

            await EnsureMovieExistsAsync(request.MovieId, cancellationToken);

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            entity.Title = request.Title.Trim();
            entity.Content = request.Content.Trim();
            entity.HasSpoiler = request.HasSpoiler;
            entity.MovieId = request.MovieId;
            await db.SaveChangesAsync(cancellationToken);

            await SetTagsAsync(entity.Id, request.Tags, cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return await GetByIdAsync(id, cancellationToken);
        }

        public override async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await db.Threads.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
            if (entity == null)
                return false;

            EnsureOwnerOrAdmin(entity.UserId);

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            var commentIds = db.Comments.Where(c => c.ThreadId == id).Select(c => c.Id);
            await db.CommentVotes.Where(v => commentIds.Contains(v.CommentId)).ExecuteDeleteAsync(cancellationToken);
            await db.Reports
                .Where(r => r.TargetType == ReportTargetType.Comment && commentIds.Contains(r.TargetId))
                .ExecuteDeleteAsync(cancellationToken);
            await db.Comments.Where(c => c.ThreadId == id).ExecuteDeleteAsync(cancellationToken);
            await db.ThreadVotes.Where(v => v.ThreadId == id).ExecuteDeleteAsync(cancellationToken);
            await db.ThreadTags.Where(t => t.ThreadId == id).ExecuteDeleteAsync(cancellationToken);
            await db.Reports
                .Where(r => r.TargetType == ReportTargetType.Thread && r.TargetId == id)
                .ExecuteDeleteAsync(cancellationToken);

            db.Threads.Remove(entity);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }

        public async Task<ThreadResponse?> UpdateStatusAsync(int id, ThreadStatus status, CancellationToken cancellationToken = default)
        {
            var entity = await db.Threads.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
            if (entity == null)
                return null;

            if (entity.Status != status)
            {
                if (!AllowedTransitions[entity.Status].Contains(status))
                    throw new BusinessException($"Prelaz statusa diskusije iz '{entity.Status}' u '{status}' nije dozvoljen.");

                entity.Status = status;
                await db.SaveChangesAsync(cancellationToken);

                if (status is ThreadStatus.Locked or ThreadStatus.Removed)
                {
                    var action = status == ThreadStatus.Locked ? "zaključana" : "uklonjena";
                    await notifier.NotifyAsync(
                        [entity.UserId],
                        $"Diskusija {action}",
                        $"Vaša diskusija '{entity.Title}' je {action} od strane moderatora.",
                        new Dictionary<string, string> { ["type"] = "thread_status", ["threadId"] = id.ToString() },
                        cancellationToken);
                }
            }

            return await GetByIdAsync(id, cancellationToken);
        }

        public async Task<ThreadResponse> VoteAsync(int threadId, bool isUpvote, CancellationToken cancellationToken = default)
        {
            var userId = identity.GetCurrentUserId();
            var thread = await db.Threads.AsNoTracking().FirstOrDefaultAsync(t => t.Id == threadId, cancellationToken)
                ?? throw new NotFoundException("Diskusija", threadId);

            if (thread.Status == ThreadStatus.Removed)
                throw new NotFoundException("Diskusija", threadId);

            var vote = await db.ThreadVotes.FirstOrDefaultAsync(v => v.ThreadId == threadId && v.UserId == userId, cancellationToken);
            if (vote == null)
                db.ThreadVotes.Add(new ThreadVote { ThreadId = threadId, UserId = userId, IsUpvote = isUpvote });
            else
                vote.IsUpvote = isUpvote;

            await db.SaveChangesAsync(cancellationToken);
            return (await GetByIdAsync(threadId, cancellationToken))!;
        }

        public async Task<ThreadResponse> RemoveVoteAsync(int threadId, CancellationToken cancellationToken = default)
        {
            var userId = identity.GetCurrentUserId();
            await db.ThreadVotes
                .Where(v => v.ThreadId == threadId && v.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);

            return await GetByIdAsync(threadId, cancellationToken) ?? throw new NotFoundException("Diskusija", threadId);
        }

        public async Task<List<string>> GetPopularTagsAsync(int count, CancellationToken cancellationToken = default)
        {
            count = Math.Clamp(count, 1, 50);
            return await db.ThreadTags
                .Where(tt => tt.Thread.Status != ThreadStatus.Removed)
                .GroupBy(tt => tt.Tag.Name)
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.Key)
                .Select(g => g.Key)
                .Take(count)
                .ToListAsync(cancellationToken);
        }

        private async Task SetTagsAsync(int threadId, IEnumerable<string> rawTags, CancellationToken ct)
        {
            var names = ParseTags(rawTags);

            await db.ThreadTags.Where(t => t.ThreadId == threadId).ExecuteDeleteAsync(ct);
            if (names.Count == 0)
                return;

            var existing = await db.Tags.Where(t => names.Contains(t.Name)).ToListAsync(ct);
            foreach (var name in names.Where(n => existing.All(t => t.Name != n)))
            {
                var tag = new Tag { Name = name };
                db.Tags.Add(tag);
                existing.Add(tag);
            }

            foreach (var tag in existing)
                db.ThreadTags.Add(new ThreadTag { ThreadId = threadId, Tag = tag });
            await db.SaveChangesAsync(ct);
        }

        private static List<string> ParseTags(string? commaSeparated) =>
            ParseTags((commaSeparated ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries));

        private static List<string> ParseTags(IEnumerable<string> raw) =>
            raw.Select(t => t.Trim().TrimStart('#').ToLowerInvariant())
                .Where(t => t.Length > 0)
                .Select(t => t.Length > 50 ? t[..50] : t)
                .Distinct()
                .ToList();

        private async Task EnsureMovieExistsAsync(int? movieId, CancellationToken ct)
        {
            if (movieId != null && !await db.Movies.AnyAsync(m => m.Id == movieId, ct))
                throw new NotFoundException("Film", movieId.Value);
        }

        private void EnsureOwnerOrAdmin(int ownerId)
        {
            if (!identity.IsAdmin() && identity.GetCurrentUserId() != ownerId)
                throw new UnauthorizedAccessException("Možete mijenjati samo vlastite diskusije.");
        }
    }
}
