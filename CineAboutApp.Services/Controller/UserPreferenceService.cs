using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Interfaces.General;
using CineAboutApp.Model;
using CineAboutApp.Models.Requests.User;
using CineAboutApp.Models.Responses.User;
using CineAboutApp.Services.Database;
using Microsoft.EntityFrameworkCore;

namespace CineAboutApp.Services.Controller
{
    public class UserPreferenceService(ApplicationDbContext db, IIdentityService identity) : IUserPreferenceService
    {
        public async Task<GenrePreferencesResponse> GetGenresAsync(CancellationToken cancellationToken = default)
        {
            var userId = identity.GetCurrentUserId();
            var ids = await db.UserGenrePreferences.AsNoTracking()
                .Where(p => p.UserId == userId)
                .Select(p => p.GenreId)
                .ToListAsync(cancellationToken);

            return new GenrePreferencesResponse { GenreIds = ids };
        }

        public async Task<GenrePreferencesResponse> SetGenresAsync(GenrePreferencesRequest request, CancellationToken cancellationToken = default)
        {
            var userId = identity.GetCurrentUserId();
            var requested = request.GenreIds.Distinct().ToList();

            var validIds = await db.Genres.AsNoTracking()
                .Where(g => requested.Contains(g.Id))
                .Select(g => g.Id)
                .ToListAsync(cancellationToken);

            if (validIds.Count != requested.Count)
                throw new BusinessException("Odabrani žanr ne postoji.");

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            await db.UserGenrePreferences.Where(p => p.UserId == userId).ExecuteDeleteAsync(cancellationToken);
            db.UserGenrePreferences.AddRange(validIds.Select(id => new UserGenrePreference { UserId = userId, GenreId = id }));
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new GenrePreferencesResponse { GenreIds = validIds };
        }
    }
}
