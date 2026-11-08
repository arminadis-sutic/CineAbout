using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Interfaces.General;
using CineAboutApp.Model;
using CineAboutApp.Models.Enums;
using CineAboutApp.Models.Responses;
using CineAboutApp.Services.Database;
using CineAboutApp.Services.General;
using Microsoft.EntityFrameworkCore;

namespace CineAboutApp.Services.Controller
{
    public class RecommendationService(ApplicationDbContext db, IIdentityService identity) : IRecommendationService
    {
        private const double GenreWeight = 0.5;
        private const double SimilarUsersWeight = 0.3;
        private const double PopularityWeight = 0.2;

        private const double PreferredGenreAffinity = 1.0;
        private const double SwipeRightAffinity = 0.5;
        private const double SwipeLeftAffinity = -0.5;
        private const double ListAffinity = 0.3;

        private const int MinCommonRatings = 2;
        private const double MinSimilarity = 0.6;
        private const int MaxSimilarUsers = 20;
        private const int BayesianPriorVotes = 3;
        private const int LikedRating = 7;
        private const int MaxCards = 20;

        private sealed record Scored(int MovieId, double Score, double Genre, double Similar, double Popularity, bool HasSimilar);

        public async Task<List<DiscoverCardResponse>> GetDiscoverAsync(int count, CancellationToken cancellationToken = default)
        {
            var userId = identity.GetCurrentUserId();
            count = Math.Clamp(count, 1, MaxCards);

            var myReviews = await db.Reviews.AsNoTracking()
                .Where(r => r.UserId == userId && r.Status != ReviewStatus.Removed)
                .Select(r => new { r.MovieId, r.Rating })
                .ToListAsync(cancellationToken);
            var swipes = await db.SwipeHistories.AsNoTracking()
                .Where(s => s.UserId == userId)
                .Select(s => new { s.MovieId, s.Direction })
                .ToListAsync(cancellationToken);
            var lists = await db.WatchlistItems.AsNoTracking()
                .Where(w => w.UserId == userId)
                .Select(w => w.MovieId)
                .ToListAsync(cancellationToken);
            var preferredGenres = await db.UserGenrePreferences.AsNoTracking()
                .Where(p => p.UserId == userId)
                .Select(p => p.GenreId)
                .ToListAsync(cancellationToken);

            var excluded = myReviews.Select(r => r.MovieId)
                .Concat(swipes.Select(s => s.MovieId))
                .Concat(lists)
                .ToHashSet();

            var signalMovieIds = excluded.ToList();
            var movieGenres = signalMovieIds.Count == 0
                ? new Dictionary<int, List<int>>()
                : (await db.MovieGenres.AsNoTracking()
                        .Where(mg => signalMovieIds.Contains(mg.MovieId))
                        .Select(mg => new { mg.MovieId, mg.GenreId })
                        .ToListAsync(cancellationToken))
                    .GroupBy(x => x.MovieId)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.GenreId).ToList());

            var affinity = new Dictionary<int, double>();
            void AddAffinity(int genreId, double value) =>
                affinity[genreId] = affinity.GetValueOrDefault(genreId) + value;

            foreach (var genreId in preferredGenres)
                AddAffinity(genreId, PreferredGenreAffinity);

            foreach (var review in myReviews)
                foreach (var genreId in movieGenres.GetValueOrDefault(review.MovieId) ?? [])
                    AddAffinity(genreId, (review.Rating - 5) / 5.0);

            foreach (var swipe in swipes)
                foreach (var genreId in movieGenres.GetValueOrDefault(swipe.MovieId) ?? [])
                    AddAffinity(genreId, swipe.Direction == SwipeDirection.Right ? SwipeRightAffinity : SwipeLeftAffinity);

            foreach (var movieId in lists)
                foreach (var genreId in movieGenres.GetValueOrDefault(movieId) ?? [])
                    AddAffinity(genreId, ListAffinity);

            var maxAffinity = affinity.Count == 0 ? 0 : affinity.Values.Max(Math.Abs);
            var hasGenreSignal = maxAffinity > 0;

            var candidates = await db.Movies.AsNoTracking()
                .Where(m => m.IsVisible && !excluded.Contains(m.Id))
                .Select(m => new { m.Id, GenreIds = m.MovieGenres.Select(g => g.GenreId).ToList() })
                .ToListAsync(cancellationToken);

            if (candidates.Count == 0)
                return [];

            var similarScores = await ComputeSimilarUserScoresAsync(userId, myReviews.ToDictionary(r => r.MovieId, r => r.Rating), cancellationToken);

            var ratingRows = await db.Reviews.AsNoTracking()
                .Where(r => r.Status != ReviewStatus.Removed)
                .GroupBy(r => r.MovieId)
                .Select(g => new { MovieId = g.Key, Avg = g.Average(r => (double)r.Rating), Count = g.Count() })
                .ToListAsync(cancellationToken);
            var ratings = ratingRows.ToDictionary(r => r.MovieId, r => new RatingStat(r.Avg, r.Count));
            var globalMean = ratingRows.Count == 0
                ? 5.0
                : ratingRows.Sum(r => r.Avg * r.Count) / ratingRows.Sum(r => r.Count);

            double Popularity(int movieId)
            {
                if (!ratings.TryGetValue(movieId, out var r))
                    return globalMean / 10.0;
                var weighted = (r.Count * r.Avg + BayesianPriorVotes * globalMean) / (r.Count + BayesianPriorVotes);
                return weighted / 10.0;
            }

            var coldStart = !hasGenreSignal && similarScores.Count == 0;
            var scored = candidates.Select(c =>
            {
                var genre = GenreScore(c.GenreIds, affinity, maxAffinity, hasGenreSignal);
                var hasSimilar = similarScores.TryGetValue(c.Id, out var similar);
                var popularity = Popularity(c.Id);

                var score = coldStart
                    ? popularity
                    : hasSimilar
                        ? GenreWeight * genre + SimilarUsersWeight * similar.Score + PopularityWeight * popularity
                        : (GenreWeight * genre + PopularityWeight * popularity) / (GenreWeight + PopularityWeight);

                return new Scored(c.Id, score, genre, hasSimilar ? similar.Score : 0, popularity, hasSimilar);
            })
            .OrderByDescending(s => s.Score)
            .ThenBy(s => s.MovieId)
            .Take(count)
            .ToList();

            var topIds = scored.Select(s => s.MovieId).ToList();
            var cards = await MovieStats.BuildCardsAsync(db, topIds, cancellationToken);
            var genreNames = await db.Genres.AsNoTracking().ToDictionaryAsync(g => g.Id, g => g.Name, cancellationToken);
            var candidateGenres = candidates.ToDictionary(c => c.Id, c => c.GenreIds);

            var likedIds = myReviews.Where(r => r.Rating >= LikedRating).Select(r => r.MovieId).ToList();
            var likedTitles = likedIds.Count == 0
                ? new Dictionary<int, string>()
                : await db.Movies.AsNoTracking()
                    .Where(m => likedIds.Contains(m.Id))
                    .ToDictionaryAsync(m => m.Id, m => m.Title, cancellationToken);
            var anchors = myReviews
                .Where(r => r.Rating >= LikedRating && likedTitles.ContainsKey(r.MovieId))
                .Select(r => new Anchor(likedTitles[r.MovieId], r.Rating, movieGenres.GetValueOrDefault(r.MovieId) ?? []))
                .ToList();
            var preferred = preferredGenres.ToHashSet();

            return scored
                .Where(s => cards.ContainsKey(s.MovieId))
                .Select(s => new DiscoverCardResponse
                {
                    Movie = cards[s.MovieId],
                    Score = Math.Round(s.Score, 3),
                    GenreScore = Math.Round(s.Genre, 3),
                    SimilarUsersScore = Math.Round(s.Similar, 3),
                    PopularityScore = Math.Round(s.Popularity, 3),
                    Reason = BuildReason(
                        s, coldStart, candidateGenres[s.MovieId], affinity, genreNames, preferred, anchors,
                        similarScores.GetValueOrDefault(s.MovieId), ratings.GetValueOrDefault(s.MovieId))
                })
                .ToList();
        }

        public async Task SwipeAsync(int movieId, SwipeDirection direction, CancellationToken cancellationToken = default)
        {
            var userId = identity.GetCurrentUserId();

            if (!await db.Movies.AnyAsync(m => m.Id == movieId && m.IsVisible, cancellationToken))
                throw new NotFoundException("Film", movieId);

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            var swipe = await db.SwipeHistories.FirstOrDefaultAsync(s => s.UserId == userId && s.MovieId == movieId, cancellationToken);
            if (swipe == null)
            {
                db.SwipeHistories.Add(new SwipeHistory
                {
                    UserId = userId,
                    MovieId = movieId,
                    Direction = direction,
                    SwipedAt = DateTime.UtcNow
                });
            }
            else
            {
                swipe.Direction = direction;
                swipe.SwipedAt = DateTime.UtcNow;
            }

            if (direction == SwipeDirection.Right
                && !await db.WatchlistItems.AnyAsync(w => w.UserId == userId && w.MovieId == movieId, cancellationToken))
            {
                db.WatchlistItems.Add(new WatchlistItem
                {
                    UserId = userId,
                    MovieId = movieId,
                    ListType = WatchlistType.Interested,
                    AddedAt = DateTime.UtcNow
                });
            }

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        private static double GenreScore(List<int> genreIds, Dictionary<int, double> affinity, double maxAffinity, bool hasSignal)
        {
            if (!hasSignal || genreIds.Count == 0)
                return 0.5;

            var mean = genreIds.Average(g => affinity.GetValueOrDefault(g) / maxAffinity);
            return (Math.Clamp(mean, -1, 1) + 1) / 2;
        }

        private sealed record SimilarScore(double Score, int Users);

        private async Task<Dictionary<int, SimilarScore>> ComputeSimilarUserScoresAsync(
            int userId, Dictionary<int, int> myRatings, CancellationToken ct)
        {
            if (myRatings.Count < MinCommonRatings)
                return [];

            var myMovieIds = myRatings.Keys.ToList();
            var overlap = await db.Reviews.AsNoTracking()
                .Where(r => r.UserId != userId && r.Status != ReviewStatus.Removed && myMovieIds.Contains(r.MovieId))
                .Select(r => new { r.UserId, r.MovieId, r.Rating })
                .ToListAsync(ct);

            var similarity = overlap
                .GroupBy(r => r.UserId)
                .Where(g => g.Count() >= MinCommonRatings)
                .Select(g => new { UserId = g.Key, Similarity = 1 - g.Average(r => Math.Abs(r.Rating - myRatings[r.MovieId])) / 10.0 })
                .Where(x => x.Similarity >= MinSimilarity)
                .OrderByDescending(x => x.Similarity)
                .Take(MaxSimilarUsers)
                .ToDictionary(x => x.UserId, x => x.Similarity);

            if (similarity.Count == 0)
                return [];

            var similarUserIds = similarity.Keys.ToList();
            var theirRatings = await db.Reviews.AsNoTracking()
                .Where(r => similarUserIds.Contains(r.UserId) && r.Status != ReviewStatus.Removed)
                .Select(r => new { r.UserId, r.MovieId, r.Rating })
                .ToListAsync(ct);

            return theirRatings
                .Where(r => !myRatings.ContainsKey(r.MovieId))
                .GroupBy(r => r.MovieId)
                .ToDictionary(
                    g => g.Key,
                    g => new SimilarScore(
                        g.Sum(r => similarity[r.UserId] * (r.Rating / 10.0)) / g.Sum(r => similarity[r.UserId]),
                        g.Count()));
        }

        private static string BuildReason(
            Scored s,
            bool coldStart,
            List<int> genreIds,
            Dictionary<int, double> affinity,
            Dictionary<int, string> genreNames,
            HashSet<int> preferredGenres,
            List<Anchor> anchors,
            SimilarScore? similar,
            RatingStat? rating)
        {
            if (!coldStart)
            {
                if (similar != null && s.Similar >= 0.6)
                {
                    var who = similar.Users == 1 ? "korisniku" : "korisnika";
                    return $"Sviđa se {similar.Users} {who} sličnog ukusa (njihova prosječna ocjena: {s.Similar * 10:0.0}).";
                }

                if (s.Genre >= 0.6 && genreIds.Count > 0)
                {
                    var topGenreId = genreIds.OrderByDescending(g => affinity.GetValueOrDefault(g)).First();
                    var genreName = genreNames.GetValueOrDefault(topGenreId, "omiljeni žanr");

                    var anchor = anchors
                        .Where(a => a.GenreIds.Contains(topGenreId))
                        .OrderByDescending(a => a.Rating)
                        .FirstOrDefault();
                    if (anchor != null)
                        return $"Jer ste film '{anchor.Title}' ocijenili sa {anchor.Rating}/10 (zajednički žanr: {genreName}).";

                    if (preferredGenres.Contains(topGenreId))
                        return $"Odgovara vašem omiljenom žanru: {genreName}.";

                    return $"Slično naslovima koji vas zanimaju (žanr: {genreName}).";
                }
            }

            return rating == null
                ? "Novi naslov u zajednici."
                : $"Popularno u zajednici (prosječna ocjena {rating.Avg:0.0}, recenzija: {rating.Count}).";
        }

        private sealed record RatingStat(double Avg, int Count);

        private sealed record Anchor(string Title, int Rating, List<int> GenreIds);
    }
}
