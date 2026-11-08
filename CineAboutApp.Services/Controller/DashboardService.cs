using CineAboutApp.Models.Enums;
using CineAboutApp.Models.Responses;
using CineAboutApp.Services.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CineAboutApp.Services.Controller
{
    public class DashboardService(ApplicationDbContext db, IMemoryCache cache)
    {
        private const string KpisCacheKey = "cineabout_dashboard_kpis";
        private const string ActivityCacheKeyPrefix = "cineabout_dashboard_activity_";
        private const string PopularMoviesCacheKeyPrefix = "cineabout_dashboard_popular_movies_";
        private const string ActiveThreadsCacheKeyPrefix = "cineabout_dashboard_active_threads_";

        public async Task<DashboardKpiResponse> GetKpisAsync(CancellationToken ct = default)
        {
            if (cache.TryGetValue(KpisCacheKey, out DashboardKpiResponse? cached) &&
                cached is not null)
            {
                return cached;
            }

            var now = DateTime.UtcNow;
            var currentPeriodStart = now.Date.AddDays(-7);
            var previousPeriodStart = currentPeriodStart.AddDays(-7);

            var result = new DashboardKpiResponse
            {
                TotalUsers = await db.MyAppUsers.CountAsync(u => !u.IsDeleted, ct),
                NewUsersLast7Days = await db.MyAppUsers
                    .CountAsync(u => !u.IsDeleted && u.RegistrationDate >= currentPeriodStart, ct),

                TotalReviews = await db.Reviews.CountAsync(ct),

                ActiveThreads = await db.Threads
                    .CountAsync(t => t.Status == ThreadStatus.Active, ct),

                TotalMovies = await db.Movies.CountAsync(ct),

                TotalQuizSessions = await db.QuizSessions.CountAsync(ct),

                UsersTrendPercent = await CalculateTrendPercentAsync(
                    db.MyAppUsers
                        .Where(u => u.RegistrationDate >= previousPeriodStart &&
                                    u.RegistrationDate < now)
                        .Select(u => u.RegistrationDate),
                    currentPeriodStart,
                    now,
                    ct),

                ReviewsTrendPercent = await CalculateTrendPercentAsync(
                    db.Reviews
                        .Where(r => r.CreatedAt >= previousPeriodStart &&
                                    r.CreatedAt < now)
                        .Select(r => r.CreatedAt),
                    currentPeriodStart,
                    now,
                    ct),

                ThreadsTrendPercent = await CalculateTrendPercentAsync(
                    db.Threads
                        .Where(t => t.CreatedAt >= previousPeriodStart &&
                                    t.CreatedAt < now)
                        .Select(t => t.CreatedAt),
                    currentPeriodStart,
                    now,
                    ct),

                QuizSessionsTrendPercent = await CalculateTrendPercentAsync(
                    db.QuizSessions
                        .Where(q => q.PlayedAt >= previousPeriodStart &&
                                    q.PlayedAt < now)
                        .Select(q => q.PlayedAt),
                    currentPeriodStart,
                    now,
                    ct),

                LastUpdated = now
            };

            cache.Set(KpisCacheKey, result, TimeSpan.FromSeconds(30));

            return result;
        }

        public async Task<List<DashboardActivityResponse>> GetActivityAsync(
            int days = 30,
            CancellationToken ct = default)
        {
            days = Math.Clamp(days, 7, 90);

            var cacheKey = $"{ActivityCacheKeyPrefix}{days}";
            if (cache.TryGetValue(cacheKey, out List<DashboardActivityResponse>? cached) &&
                cached is not null)
            {
                return cached;
            }

            var today = DateTime.UtcNow.Date;
            var startDate = today.AddDays(-(days - 1));
            var endDateExclusive = today.AddDays(1);

            var usersByDay = await db.MyAppUsers
                .Where(u => u.RegistrationDate >= startDate &&
                            u.RegistrationDate < endDateExclusive)
                .GroupBy(u => u.RegistrationDate.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Date, x => x.Count, ct);

            var reviewsByDay = await db.Reviews
                .Where(r => r.CreatedAt >= startDate &&
                            r.CreatedAt < endDateExclusive)
                .GroupBy(r => r.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Date, x => x.Count, ct);

            var threadsByDay = await db.Threads
                .Where(t => t.CreatedAt >= startDate &&
                            t.CreatedAt < endDateExclusive)
                .GroupBy(t => t.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Date, x => x.Count, ct);

            var commentsByDay = await db.Comments
                .Where(c => c.CreatedAt >= startDate &&
                            c.CreatedAt < endDateExclusive)
                .GroupBy(c => c.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Date, x => x.Count, ct);

            var quizzesByDay = await db.QuizSessions
                .Where(q => q.PlayedAt >= startDate &&
                            q.PlayedAt < endDateExclusive)
                .GroupBy(q => q.PlayedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Date, x => x.Count, ct);

            var result = Enumerable.Range(0, days)
                .Select(offset =>
                {
                    var date = startDate.AddDays(offset);

                    return new DashboardActivityResponse
                    {
                        Date = date,
                        NewUsers = usersByDay.GetValueOrDefault(date),
                        Reviews = reviewsByDay.GetValueOrDefault(date),
                        Threads = threadsByDay.GetValueOrDefault(date),
                        Comments = commentsByDay.GetValueOrDefault(date),
                        QuizSessions = quizzesByDay.GetValueOrDefault(date)
                    };
                })
                .ToList();

            cache.Set(cacheKey, result, TimeSpan.FromSeconds(30));

            return result;
        }

        public async Task<List<PopularMovieResponse>> GetPopularMoviesAsync(
            int limit = 10,
            CancellationToken ct = default)
        {
            limit = Math.Clamp(limit, 1, 20);

            var cacheKey = $"{PopularMoviesCacheKeyPrefix}{limit}";
            if (cache.TryGetValue(cacheKey, out List<PopularMovieResponse>? cached) &&
                cached is not null)
            {
                return cached;
            }

            var result = await db.Movies
                .AsNoTracking()
                .Where(m => m.IsVisible)
                .Select(m => new PopularMovieResponse
                {
                    MovieId = m.Id,
                    Title = m.Title,
                    PosterUrl = m.PosterUrl,

                    ReviewCount = m.Reviews.Count(),

                    AverageRating = m.Reviews.Any()
                        ? m.Reviews.Average(r => r.Rating)
                        : 0,

                    WatchlistCount = m.WatchlistItems.Count(
                        w => w.ListType == WatchlistType.Watchlist),

                    InterestedCount = m.WatchlistItems.Count(
                        w => w.ListType == WatchlistType.Interested)
                })
                .OrderByDescending(x =>
                    (x.ReviewCount * 3) +
                    (x.WatchlistCount * 2) +
                    x.InterestedCount)
                .ThenByDescending(x => x.AverageRating)
                .ThenBy(x => x.Title)
                .Take(limit)
                .ToListAsync(ct);

            foreach (var movie in result)
            {
                movie.PopularityScore =
                    (movie.ReviewCount * 3) +
                    (movie.WatchlistCount * 2) +
                    movie.InterestedCount;
            }

            cache.Set(cacheKey, result, TimeSpan.FromSeconds(30));

            return result;
        }

        public async Task<List<ActiveThreadResponse>> GetActiveThreadsAsync(
            int limit = 10,
            CancellationToken ct = default)
        {
            limit = Math.Clamp(limit, 1, 20);

            var cacheKey = $"{ActiveThreadsCacheKeyPrefix}{limit}";
            if (cache.TryGetValue(cacheKey, out List<ActiveThreadResponse>? cached) &&
                cached is not null)
            {
                return cached;
            }

            var result = await db.Threads
                .AsNoTracking()
                .Where(t => t.Status == ThreadStatus.Active)
                .Select(t => new ActiveThreadResponse
                {
                    ThreadId = t.Id,
                    Title = t.Title,
                    AuthorUsername = t.User.Username,
                    CreatedAt = t.CreatedAt,
                    HasSpoiler = t.HasSpoiler,
                    CommentCount = t.Comments.Count(),
                    VoteCount = t.ThreadVotes.Count()
                })
                .OrderByDescending(t => t.CommentCount)
                .ThenByDescending(t => t.VoteCount)
                .ThenByDescending(t => t.CreatedAt)
                .Take(limit)
                .ToListAsync(ct);

            cache.Set(cacheKey, result, TimeSpan.FromSeconds(30));

            return result;
        }

        private static async Task<int> CalculateTrendPercentAsync(
            IQueryable<DateTime> dateQuery,
            DateTime currentPeriodStart,
            DateTime now,
            CancellationToken ct)
        {
            var previousPeriodStart = currentPeriodStart.AddDays(-7);

            var counts = await dateQuery
                .GroupBy(date => date >= currentPeriodStart ? 1 : 0)
                .Select(group => new
                {
                    Period = group.Key,
                    Count = group.Count()
                })
                .ToListAsync(ct);

            var currentCount = counts
                .FirstOrDefault(x => x.Period == 1)?.Count ?? 0;

            var previousCount = counts
                .FirstOrDefault(x => x.Period == 0)?.Count ?? 0;

            if (previousCount == 0)
                return currentCount == 0 ? 0 : 100;

            return (int)Math.Round(
                ((currentCount - previousCount) / (double)previousCount) * 100,
                MidpointRounding.AwayFromZero);
        }
    }
}