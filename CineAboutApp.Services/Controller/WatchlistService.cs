using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Interfaces.General;
using CineAboutApp.Model;
using CineAboutApp.Models.Enums;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;
using CineAboutApp.Services.Database;
using CineAboutApp.Services.General;
using Microsoft.EntityFrameworkCore;

namespace CineAboutApp.Services.Controller
{
    public class WatchlistService(ApplicationDbContext db, IIdentityService identity) : IWatchlistService
    {
        public async Task<MyPagedList<WatchlistItemResponse>> GetForUserAsync(
            int userId, WatchlistSearchObject search, CancellationToken cancellationToken = default)
        {
            await EnsureCanViewAsync(userId, cancellationToken);

            var query = db.WatchlistItems.AsNoTracking()
                .Where(w => w.UserId == userId && w.Movie.IsVisible);

            if (search.ListType.HasValue)
                query = query.Where(w => w.ListType == search.ListType.Value);

            if (search.Type.HasValue)
                query = query.Where(w => w.Movie.Type == search.Type.Value);

            if (!string.IsNullOrWhiteSpace(search.NameFTS))
            {
                var term = search.NameFTS.Trim().ToLower();
                query = query.Where(w => w.Movie.Title.ToLower().Contains(term));
            }

            var pageNumber = Math.Max(search.PageNumber.GetValueOrDefault(1), 1);
            var pageSize = Math.Clamp(search.PageSize.GetValueOrDefault(20), 1, BaseSearchObject.MaxPageSize);

            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderByDescending(w => w.AddedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(w => new { w.Id, w.ListType, w.AddedAt, w.MovieId })
                .ToListAsync(cancellationToken);

            var cards = await MovieStats.BuildCardsAsync(db, items.Select(i => i.MovieId).ToList(), cancellationToken);

            var data = items
                .Where(i => cards.ContainsKey(i.MovieId))
                .Select(i => new WatchlistItemResponse
                {
                    Id = i.Id,
                    ListType = i.ListType,
                    AddedAt = i.AddedAt,
                    Movie = cards[i.MovieId]
                })
                .ToList();

            return new MyPagedList<WatchlistItemResponse>(data, total, pageNumber, pageSize);
        }

        public async Task<WatchlistItemResponse> SetAsync(int movieId, WatchlistType listType, CancellationToken cancellationToken = default)
        {
            var userId = identity.GetCurrentUserId();

            if (!await db.Movies.AnyAsync(m => m.Id == movieId && m.IsVisible, cancellationToken))
                throw new NotFoundException("Film", movieId);

            var item = await db.WatchlistItems.FirstOrDefaultAsync(w => w.UserId == userId && w.MovieId == movieId, cancellationToken);
            if (item == null)
            {
                item = new WatchlistItem
                {
                    UserId = userId,
                    MovieId = movieId,
                    ListType = listType,
                    AddedAt = DateTime.UtcNow
                };
                db.WatchlistItems.Add(item);
            }
            else
            {
                item.ListType = listType;
            }

            await db.SaveChangesAsync(cancellationToken);

            var cards = await MovieStats.BuildCardsAsync(db, [movieId], cancellationToken);
            return new WatchlistItemResponse
            {
                Id = item.Id,
                ListType = item.ListType,
                AddedAt = item.AddedAt,
                Movie = cards[movieId]
            };
        }

        public async Task RemoveAsync(int movieId, CancellationToken cancellationToken = default)
        {
            var userId = identity.GetCurrentUserId();
            var removed = await db.WatchlistItems
                .Where(w => w.UserId == userId && w.MovieId == movieId)
                .ExecuteDeleteAsync(cancellationToken);

            if (removed == 0)
                throw new NotFoundException("Film nije na vašoj listi.");
        }

        private async Task EnsureCanViewAsync(int userId, CancellationToken ct)
        {
            if (identity.IsAdmin() || identity.GetCurrentUserIdNullable() == userId)
                return;

            var isPublic = await db.UserProfiles.AsNoTracking()
                .Where(p => p.UserId == userId)
                .Select(p => (bool?)p.IsProfilePublic)
                .FirstOrDefaultAsync(ct);

            if (isPublic != true)
                throw new NotFoundException("Profil nije pronađen ili je privatan.");
        }
    }
}
