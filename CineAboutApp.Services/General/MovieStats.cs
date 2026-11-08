using CineAboutApp.Models.Enums;
using CineAboutApp.Models.Responses;
using CineAboutApp.Services.Database;
using Microsoft.EntityFrameworkCore;

namespace CineAboutApp.Services.General
{
    public static class MovieStats
    {
        public static async Task<Dictionary<int, (double Average, int Count)>> GetRatingsAsync(
            ApplicationDbContext db, IReadOnlyCollection<int> movieIds, CancellationToken ct)
        {
            if (movieIds.Count == 0)
                return new Dictionary<int, (double, int)>();

            var rows = await db.Reviews
                .Where(r => movieIds.Contains(r.MovieId) && r.Status != ReviewStatus.Removed)
                .GroupBy(r => r.MovieId)
                .Select(g => new { MovieId = g.Key, Avg = g.Average(r => (double)r.Rating), Count = g.Count() })
                .ToListAsync(ct);

            return rows.ToDictionary(r => r.MovieId, r => (Math.Round(r.Avg, 1), r.Count));
        }

        public static async Task<Dictionary<int, MovieCardResponse>> BuildCardsAsync(
            ApplicationDbContext db, IReadOnlyCollection<int> movieIds, CancellationToken ct)
        {
            var movies = await db.Movies
                .AsNoTracking()
                .Where(m => movieIds.Contains(m.Id))
                .Select(m => new
                {
                    m.Id,
                    m.Title,
                    m.Type,
                    m.ReleaseYear,
                    m.PosterUrl,
                    Genres = m.MovieGenres.Select(g => g.Genre.Name).ToList()
                })
                .ToListAsync(ct);

            var ratings = await GetRatingsAsync(db, movieIds, ct);

            return movies.ToDictionary(m => m.Id, m =>
            {
                ratings.TryGetValue(m.Id, out var rating);
                return new MovieCardResponse
                {
                    Id = m.Id,
                    Title = m.Title,
                    Type = m.Type,
                    ReleaseYear = m.ReleaseYear,
                    PosterUrl = m.PosterUrl,
                    Genres = m.Genres,
                    AverageRating = rating.Average,
                    ReviewCount = rating.Count
                };
            });
        }
    }
}
