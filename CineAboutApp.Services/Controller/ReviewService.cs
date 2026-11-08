using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Interfaces.General;
using CineAboutApp.Model;
using CineAboutApp.Models.Enums;
using CineAboutApp.Models.Requests.Review;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;
using CineAboutApp.Services.Database;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace CineAboutApp.Services.Controller
{
    public class ReviewService(
        ApplicationDbContext db,
        IMapper mapper,
        IIdentityService identity,
        IUserNotifier notifier)
        : BaseCRUDService<ReviewResponse, ReviewSearchObject, ReviewInsertOrUpdateRequest, ReviewInsertOrUpdateRequest, Review>(db, mapper), IReviewService
    {
        protected override IQueryable<Review> GetIncludeQuery(IQueryable<Review> query)
        {
            return query
                .Include(r => r.User).ThenInclude(u => u.UserProfile)
                .Include(r => r.Movie)
                .Include(r => r.ReviewVotes);
        }

        public override async Task<ReviewResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var review = await base.GetByIdAsync(id, cancellationToken);
            if (review != null && review.Status != ReviewStatus.Active && !identity.IsAdmin())
            {
                return null;
            }

            return review;
        }

        protected override IQueryable<Review> ApplyFilters(IQueryable<Review> query, ReviewSearchObject search)
        {
            if (!identity.IsAdmin())
            {
                query = query.Where(x => x.Status == ReviewStatus.Active);
            }
            else if (search.Status.HasValue)
            {
                query = query.Where(x => x.Status == search.Status.Value);
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

            if (search.Rating.HasValue)
            {
                query = query.Where(x => x.Rating == search.Rating.Value);
            }

            if (search.MovieId.HasValue)
            {
                query = query.Where(x => x.MovieId == search.MovieId.Value);
            }

            if (search.UserId.HasValue)
            {
                query = query.Where(x => x.UserId == search.UserId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search.NameFTS))
            {
                var term = search.NameFTS.Trim().ToLower();
                query = query.Where(x => x.Movie.Title.ToLower().Contains(term) || x.User.Username.ToLower().Contains(term));
            }

            return query;
        }

        protected override IQueryable<Review> ApplySorting(IQueryable<Review> query, ReviewSearchObject search)
        {
            return search.SortBy switch
            {
                ReviewSortBy.Date => query.OrderByDescending(x => x.CreatedAt),
                ReviewSortBy.Rating => query.OrderByDescending(x => x.Rating).ThenByDescending(x => x.CreatedAt),
                _ => query
                    .OrderByDescending(x => x.ReviewVotes.Count(v => v.IsHelpful) - x.ReviewVotes.Count(v => !v.IsHelpful))
                    .ThenByDescending(x => x.CreatedAt)
            };
        }

        protected override ReviewResponse MapToResponse(Review entity)
        {
            var currentUserId = identity.GetCurrentUserIdNullable();
            return new ReviewResponse
            {
                Id = entity.Id,
                UserId = entity.UserId,
                Username = entity.User?.Username ?? string.Empty,
                UserImageUrl = entity.User?.UserProfile?.ProfileImageUrl,
                MovieId = entity.MovieId,
                MovieTitle = entity.Movie?.Title ?? string.Empty,
                MoviePosterUrl = entity.Movie?.PosterUrl,
                Rating = entity.Rating,
                Text = entity.Text,
                CreatedAt = entity.CreatedAt,
                Status = entity.Status,
                HelpfulCount = entity.ReviewVotes.Count(v => v.IsHelpful),
                NotHelpfulCount = entity.ReviewVotes.Count(v => !v.IsHelpful),
                MyVote = currentUserId == null
                    ? null
                    : entity.ReviewVotes.Where(v => v.UserId == currentUserId).Select(v => (bool?)v.IsHelpful).FirstOrDefault()
            };
        }

        public override async Task<ReviewResponse> CreateAsync(ReviewInsertOrUpdateRequest request, CancellationToken cancellationToken = default)
        {
            var userId = identity.GetCurrentUserId();

            var movie = await db.Movies.AsNoTracking().FirstOrDefaultAsync(m => m.Id == request.MovieId, cancellationToken)
                ?? throw new NotFoundException("Film", request.MovieId);

            if (!movie.IsVisible && !identity.IsAdmin())
                throw new NotFoundException("Film", request.MovieId);

            if (await db.Reviews.AnyAsync(r => r.UserId == userId && r.MovieId == request.MovieId, cancellationToken))
                throw new BusinessException("Već ste ocijenili ovaj film. Uredite postojeću recenziju umjesto dodavanja nove.");

            var entity = new Review
            {
                UserId = userId,
                MovieId = request.MovieId,
                Rating = request.Rating,
                Text = request.Text.Trim(),
                CreatedAt = DateTime.UtcNow,
                Status = ReviewStatus.Active
            };

            db.Reviews.Add(entity);
            await db.SaveChangesAsync(cancellationToken);

            return (await GetByIdAsync(entity.Id, cancellationToken))!;
        }

        public override async Task<ReviewResponse?> UpdateAsync(int id, ReviewInsertOrUpdateRequest request, CancellationToken cancellationToken = default)
        {
            var entity = await db.Reviews.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
            if (entity == null)
                return null;

            EnsureOwnerOrAdmin(entity.UserId);

            if (entity.MovieId != request.MovieId)
                throw new BusinessException("Film recenzije se ne može promijeniti.");

            entity.Rating = request.Rating;
            entity.Text = request.Text.Trim();
            await db.SaveChangesAsync(cancellationToken);

            return await GetByIdAsync(id, cancellationToken);
        }

        public override async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await db.Reviews.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
            if (entity == null)
                return false;

            EnsureOwnerOrAdmin(entity.UserId);

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            await db.ReviewVotes.Where(v => v.ReviewId == id).ExecuteDeleteAsync(cancellationToken);
            await db.Reports
                .Where(r => r.TargetType == ReportTargetType.Review && r.TargetId == id)
                .ExecuteDeleteAsync(cancellationToken);
            db.Reviews.Remove(entity);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }

        private static readonly Dictionary<ReviewStatus, ReviewStatus[]> AllowedTransitions = new()
        {
            [ReviewStatus.Active] = [ReviewStatus.Reported, ReviewStatus.Removed],
            [ReviewStatus.Reported] = [ReviewStatus.Active, ReviewStatus.Removed],
            [ReviewStatus.Removed] = [ReviewStatus.Active],
        };

        public async Task<ReviewResponse?> UpdateStatusAsync(int id, ReviewStatus status, CancellationToken cancellationToken = default)
        {
            var entity = await db.Reviews.Include(r => r.Movie).FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
            if (entity == null)
                return null;

            if (entity.Status == status)
                return await GetByIdAsync(id, cancellationToken);

            if (!AllowedTransitions[entity.Status].Contains(status))
                throw new BusinessException($"Prelaz recenzije iz statusa '{entity.Status}' u '{status}' nije dozvoljen.");

            entity.Status = status;
            await db.SaveChangesAsync(cancellationToken);

            if (status == ReviewStatus.Removed)
            {
                await notifier.NotifyAsync(
                    [entity.UserId],
                    "Recenzija uklonjena",
                    $"Vaša recenzija za film '{entity.Movie.Title}' uklonjena je zbog kršenja pravila zajednice.",
                    new Dictionary<string, string> { ["type"] = "review_removed", ["reviewId"] = id.ToString() },
                    cancellationToken);
            }

            return await GetByIdAsync(id, cancellationToken);
        }

        public async Task<ReviewResponse> VoteAsync(int reviewId, bool isHelpful, CancellationToken cancellationToken = default)
        {
            var userId = identity.GetCurrentUserId();
            var review = await db.Reviews.AsNoTracking().FirstOrDefaultAsync(r => r.Id == reviewId, cancellationToken)
                ?? throw new NotFoundException("Recenzija", reviewId);

            if (review.UserId == userId)
                throw new BusinessException("Ne možete glasati za vlastitu recenziju.");

            var vote = await db.ReviewVotes.FirstOrDefaultAsync(v => v.ReviewId == reviewId && v.UserId == userId, cancellationToken);
            if (vote == null)
                db.ReviewVotes.Add(new ReviewVote { ReviewId = reviewId, UserId = userId, IsHelpful = isHelpful });
            else
                vote.IsHelpful = isHelpful;

            await db.SaveChangesAsync(cancellationToken);
            return (await GetByIdAsync(reviewId, cancellationToken))!;
        }

        public async Task<ReviewResponse> RemoveVoteAsync(int reviewId, CancellationToken cancellationToken = default)
        {
            var userId = identity.GetCurrentUserId();
            await db.ReviewVotes
                .Where(v => v.ReviewId == reviewId && v.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);

            return await GetByIdAsync(reviewId, cancellationToken) ?? throw new NotFoundException("Recenzija", reviewId);
        }

        public async Task<RatingSummaryResponse> GetSummaryAsync(int movieId, CancellationToken cancellationToken = default)
        {
            if (!await db.Movies.AnyAsync(m => m.Id == movieId, cancellationToken))
                throw new NotFoundException("Film", movieId);

            var counts = await db.Reviews
                .Where(r => r.MovieId == movieId && r.Status != ReviewStatus.Removed)
                .GroupBy(r => r.Rating)
                .Select(g => new { Rating = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var histogram = new int[11];
            foreach (var row in counts.Where(c => c.Rating is >= 0 and <= 10))
                histogram[row.Rating] = row.Count;

            var total = histogram.Sum();
            var summary = new RatingSummaryResponse
            {
                MovieId = movieId,
                ReviewCount = total,
                Histogram = histogram,
                AverageRating = total == 0
                    ? 0
                    : Math.Round(histogram.Select((count, rating) => (double)count * rating).Sum() / total, 1)
            };

            var userId = identity.GetCurrentUserIdNullable();
            if (userId != null)
            {
                var mine = await db.Reviews.AsNoTracking()
                    .Where(r => r.MovieId == movieId && r.UserId == userId)
                    .Select(r => new { r.Id, r.Rating })
                    .FirstOrDefaultAsync(cancellationToken);
                summary.MyRating = mine?.Rating;
                summary.MyReviewId = mine?.Id;
            }

            return summary;
        }

        private void EnsureOwnerOrAdmin(int ownerId)
        {
            if (!identity.IsAdmin() && identity.GetCurrentUserId() != ownerId)
                throw new UnauthorizedAccessException("Možete mijenjati samo vlastite recenzije.");
        }
    }
}
