using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Services.Database;
using CineAboutApp.Services.General;
using CineAboutApp.Model;
using Microsoft.EntityFrameworkCore;

namespace CineAboutApp.Services.Controller
{
    public class ExportService(
        ApplicationDbContext db,
        ReportPdfGenerator reportPdfGenerator) : IExportService
    {
        private const int MaxReportDays = 366;

        private static (DateTime Start, DateTime EndExclusive) ReportRange(DateTime startDate, DateTime endDate)
        {
            if (startDate == default || endDate == default)
            {
                throw new UserException("Datum početka i datum kraja su obavezni.");
            }

            if (startDate.Date > endDate.Date)
            {
                throw new UserException("Datum početka ne može biti poslije datuma kraja.");
            }

            if ((endDate.Date - startDate.Date).TotalDays + 1 > MaxReportDays)
            {
                throw new UserException($"Izvještaj može obuhvatiti najviše {MaxReportDays} dana.");
            }

            return (startDate.Date, endDate.Date.AddDays(1));
        }

        public async Task<byte[]> GenerateCommunityActivityReportPdfAsync(
            DateTime startDate,
            DateTime endDate,
            CancellationToken ct = default)
        {
            var (start, endExclusive) = ReportRange(startDate, endDate);

            var registrations = await db.MyAppUsers.AsNoTracking()
                .Where(u => u.RegistrationDate >= start && u.RegistrationDate < endExclusive)
                .GroupBy(u => u.RegistrationDate.Date).Select(g => new { Day = g.Key, Count = g.Count() }).ToListAsync(ct);
            var reviews = await db.Reviews.AsNoTracking()
                .Where(r => r.CreatedAt >= start && r.CreatedAt < endExclusive)
                .GroupBy(r => r.CreatedAt.Date).Select(g => new { Day = g.Key, Count = g.Count() }).ToListAsync(ct);
            var threads = await db.Threads.AsNoTracking()
                .Where(t => t.CreatedAt >= start && t.CreatedAt < endExclusive)
                .GroupBy(t => t.CreatedAt.Date).Select(g => new { Day = g.Key, Count = g.Count() }).ToListAsync(ct);
            var quizSessions = await db.QuizSessions.AsNoTracking()
                .Where(q => q.PlayedAt >= start && q.PlayedAt < endExclusive)
                .GroupBy(q => q.PlayedAt.Date).Select(g => new { Day = g.Key, Count = g.Count() }).ToListAsync(ct);

            int Of(IEnumerable<(DateTime Day, int Count)> rows, DateTime day) => rows.Where(r => r.Day == day).Sum(r => r.Count);
            var reg = registrations.Select(r => (r.Day, r.Count)).ToList();
            var rev = reviews.Select(r => (r.Day, r.Count)).ToList();
            var thr = threads.Select(r => (r.Day, r.Count)).ToList();
            var qz = quizSessions.Select(r => (r.Day, r.Count)).ToList();

            var dailyStats = new List<(DateTime Date, int NewRegistrations, int NewReviews, int NewThreads, int QuizSessions)>();
            for (var day = start; day < endExclusive; day = day.AddDays(1))
            {
                dailyStats.Add((day, Of(reg, day), Of(rev, day), Of(thr, day), Of(qz, day)));
            }

            return await reportPdfGenerator.GenerateCommunityActivityReportPdfAsync(start, endExclusive.AddDays(-1), dailyStats, ct);
        }

        public async Task<byte[]> GenerateContentPopularityReportPdfAsync(
            DateTime startDate,
            DateTime endDate,
            CancellationToken ct = default)
        {
            var (start, endExclusive) = ReportRange(startDate, endDate);

            var movies = await db.Movies.AsNoTracking()
                .Select(m => new
                {
                    m.Title,
                    ReviewCount = m.Reviews.Count(r => r.CreatedAt >= start && r.CreatedAt < endExclusive),
                    AvgRating = m.Reviews.Any() ? m.Reviews.Average(r => (double)r.Rating) : 0,
                    WatchlistCount = m.WatchlistItems.Count(w => w.AddedAt >= start && w.AddedAt < endExclusive),
                    ThreadCount = m.Threads.Count(t => t.CreatedAt >= start && t.CreatedAt < endExclusive),
                })
                .ToListAsync(ct);

            var topMovies = movies
                .Where(m => m.ReviewCount + m.WatchlistCount + m.ThreadCount > 0)
                .OrderByDescending(m => m.ReviewCount + m.WatchlistCount + m.ThreadCount)
                .ThenBy(m => m.Title)
                .Take(10)
                .Select(m => (m.Title, m.ReviewCount, m.AvgRating, m.WatchlistCount, m.ThreadCount))
                .ToList();

            return await reportPdfGenerator.GenerateContentPopularityReportPdfAsync(start, endExclusive.AddDays(-1), topMovies, ct);
        }
    }
}
