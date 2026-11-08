using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Interfaces.General;
using CineAboutApp.Model;
using CineAboutApp.Models.Enums;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;
using CineAboutApp.Services.Database;
using Microsoft.EntityFrameworkCore;

namespace CineAboutApp.Services.Controller
{
    public class FollowService(ApplicationDbContext db, IIdentityService identity, IUserNotifier notifier) : IFollowService
    {
        private const int FeedSourceLimit = 100;

        public async Task FollowAsync(int userId, CancellationToken cancellationToken = default)
        {
            var currentUserId = identity.GetCurrentUserId();
            if (currentUserId == userId)
                throw new BusinessException("Ne možete pratiti sami sebe.");

            if (!await db.MyAppUsers.AnyAsync(u => u.Id == userId && u.IsActive && !u.IsDeleted, cancellationToken))
                throw new NotFoundException("Korisnik", userId);

            if (await db.Follows.AnyAsync(f => f.FollowerId == currentUserId && f.FollowingId == userId, cancellationToken))
                throw new BusinessException("Već pratite ovog korisnika.");

            db.Follows.Add(new Follow { FollowerId = currentUserId, FollowingId = userId, CreatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync(cancellationToken);

            var username = await db.MyAppUsers.AsNoTracking()
                .Where(u => u.Id == currentUserId).Select(u => u.Username).FirstAsync(cancellationToken);

            await notifier.NotifyAsync(
                [userId],
                "Novi pratilac",
                $"{username} vas sada prati.",
                new Dictionary<string, string> { ["type"] = "follow", ["userId"] = currentUserId.ToString() },
                cancellationToken);
        }

        public async Task UnfollowAsync(int userId, CancellationToken cancellationToken = default)
        {
            var currentUserId = identity.GetCurrentUserId();
            var removed = await db.Follows
                .Where(f => f.FollowerId == currentUserId && f.FollowingId == userId)
                .ExecuteDeleteAsync(cancellationToken);

            if (removed == 0)
                throw new BusinessException("Ne pratite ovog korisnika.");
        }

        public Task<MyPagedList<FollowUserResponse>> GetFollowersAsync(int userId, BaseSearchObject search, CancellationToken cancellationToken = default)
            => GetUsersAsync(db.Follows.Where(f => f.FollowingId == userId).Select(f => f.Follower), search, cancellationToken);

        public Task<MyPagedList<FollowUserResponse>> GetFollowingAsync(int userId, BaseSearchObject search, CancellationToken cancellationToken = default)
            => GetUsersAsync(db.Follows.Where(f => f.FollowerId == userId).Select(f => f.Following), search, cancellationToken);

        private async Task<MyPagedList<FollowUserResponse>> GetUsersAsync(
            IQueryable<MyAppUser> users, BaseSearchObject search, CancellationToken ct)
        {
            if (!string.IsNullOrWhiteSpace(search.NameFTS))
            {
                var term = search.NameFTS.Trim().ToLower();
                users = users.Where(u => u.Username.ToLower().Contains(term));
            }

            var pageNumber = Math.Max(search.PageNumber.GetValueOrDefault(1), 1);
            var pageSize = Math.Clamp(search.PageSize.GetValueOrDefault(20), 1, BaseSearchObject.MaxPageSize);

            var total = await users.CountAsync(ct);
            var page = await users
                .OrderBy(u => u.Username)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new FollowUserResponse
                {
                    UserId = u.Id,
                    Username = u.Username,
                    ProfileImageUrl = u.UserProfile != null ? u.UserProfile.ProfileImageUrl : null
                })
                .ToListAsync(ct);

            var currentUserId = identity.GetCurrentUserIdNullable();
            if (currentUserId != null && page.Count > 0)
            {
                var ids = page.Select(p => p.UserId).ToList();
                var followed = await db.Follows
                    .Where(f => f.FollowerId == currentUserId && ids.Contains(f.FollowingId))
                    .Select(f => f.FollowingId)
                    .ToListAsync(ct);

                foreach (var item in page)
                    item.IsFollowedByCurrentUser = followed.Contains(item.UserId);
            }

            return new MyPagedList<FollowUserResponse>(page, total, pageNumber, pageSize);
        }

        public async Task<MyPagedList<FeedItemResponse>> GetFeedAsync(BaseSearchObject search, CancellationToken cancellationToken = default)
        {
            var currentUserId = identity.GetCurrentUserId();
            var followedIds = db.Follows.Where(f => f.FollowerId == currentUserId).Select(f => f.FollowingId);

            var reviews = await db.Reviews.AsNoTracking()
                .Where(r => followedIds.Contains(r.UserId) && r.Status == ReviewStatus.Active && r.User.IsActive)
                .OrderByDescending(r => r.CreatedAt)
                .Take(FeedSourceLimit)
                .Select(r => new FeedItemResponse
                {
                    Type = "Review",
                    UserId = r.UserId,
                    Username = r.User.Username,
                    UserImageUrl = r.User.UserProfile != null ? r.User.UserProfile.ProfileImageUrl : null,
                    MovieId = r.MovieId,
                    MovieTitle = r.Movie.Title,
                    MoviePosterUrl = r.Movie.PosterUrl,
                    Rating = r.Rating,
                    Text = r.Text,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync(cancellationToken);

            var threads = await db.Threads.AsNoTracking()
                .Where(t => followedIds.Contains(t.UserId) && t.Status != ThreadStatus.Removed)
                .OrderByDescending(t => t.CreatedAt)
                .Take(FeedSourceLimit)
                .Select(t => new FeedItemResponse
                {
                    Type = "Thread",
                    UserId = t.UserId,
                    Username = t.User.Username,
                    UserImageUrl = t.User.UserProfile != null ? t.User.UserProfile.ProfileImageUrl : null,
                    MovieId = t.MovieId,
                    MovieTitle = t.Movie != null ? t.Movie.Title : null,
                    MoviePosterUrl = t.Movie != null ? t.Movie.PosterUrl : null,
                    Text = t.Title,
                    ThreadId = t.Id,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync(cancellationToken);

            var lists = await db.WatchlistItems.AsNoTracking()
                .Where(w => followedIds.Contains(w.UserId) && w.Movie.IsVisible)
                .OrderByDescending(w => w.AddedAt)
                .Take(FeedSourceLimit)
                .Select(w => new FeedItemResponse
                {
                    Type = w.ListType == WatchlistType.Watchlist ? "Watchlist" : "Interested",
                    UserId = w.UserId,
                    Username = w.User.Username,
                    UserImageUrl = w.User.UserProfile != null ? w.User.UserProfile.ProfileImageUrl : null,
                    MovieId = w.MovieId,
                    MovieTitle = w.Movie.Title,
                    MoviePosterUrl = w.Movie.PosterUrl,
                    CreatedAt = w.AddedAt
                })
                .ToListAsync(cancellationToken);

            var createdLists = await db.UserLists.AsNoTracking()
                .Where(l => followedIds.Contains(l.UserId) && l.User.IsActive && !l.User.IsDeleted && l.Items.Any(i => i.Movie.IsVisible))
                .OrderByDescending(l => l.CreatedAt)
                .Take(FeedSourceLimit)
                .Select(l => new FeedItemResponse
                {
                    Type = "List",
                    UserId = l.UserId,
                    Username = l.User.Username,
                    UserImageUrl = l.User.UserProfile != null ? l.User.UserProfile.ProfileImageUrl : null,
                    Text = l.Name,
                    ListId = l.Id,
                    CreatedAt = l.CreatedAt
                })
                .ToListAsync(cancellationToken);

            var merged = reviews.Concat(threads).Concat(lists).Concat(createdLists).OrderByDescending(i => i.CreatedAt).ToList();

            var pageNumber = Math.Max(search.PageNumber.GetValueOrDefault(1), 1);
            var pageSize = Math.Clamp(search.PageSize.GetValueOrDefault(20), 1, BaseSearchObject.MaxPageSize);
            var data = merged.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            return new MyPagedList<FeedItemResponse>(data, merged.Count, pageNumber, pageSize);
        }
    }
}
