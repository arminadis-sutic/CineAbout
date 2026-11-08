using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Interfaces.General;
using CineAboutApp.Model;
using CineAboutApp.Models.Requests;
using CineAboutApp.Models.Responses;
using CineAboutApp.Services.Database;
using Microsoft.EntityFrameworkCore;

namespace CineAboutApp.Services.Controller
{
    public class UserListService(ApplicationDbContext db, IIdentityService identity) : IUserListService
    {
        private const int MaxListsPerUser = 50;
        private const int MaxItemsPerList = 200;

        private IQueryable<UserList> Visible() => db.UserLists.AsNoTracking().Where(l => l.User.IsActive && !l.User.IsDeleted);

        public async Task<List<UserListSummaryResponse>> GetListsAsync(int? userId, CancellationToken cancellationToken = default)
        {
            var uid = userId ?? identity.GetCurrentUserId();
            var admin = identity.IsAdmin();
            return await Visible()
                .Where(l => l.UserId == uid)
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => new UserListSummaryResponse
                {
                    Id = l.Id,
                    UserId = l.UserId,
                    Username = l.User.Username,
                    Name = l.Name,
                    CreatedAt = l.CreatedAt,
                    ItemCount = l.Items.Count(i => admin || i.Movie.IsVisible),
                    PosterUrls = l.Items.Where(i => admin || i.Movie.IsVisible).OrderBy(i => i.AddedAt).Take(4).Select(i => i.Movie.PosterUrl).ToList()
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<UserListDetailResponse?> GetAsync(int id, CancellationToken cancellationToken = default)
        {
            var admin = identity.IsAdmin();
            var list = await Visible().Where(l => l.Id == id)
                .Select(l => new UserListDetailResponse
                {
                    Id = l.Id,
                    UserId = l.UserId,
                    Username = l.User.Username,
                    Name = l.Name,
                    CreatedAt = l.CreatedAt,
                })
                .FirstOrDefaultAsync(cancellationToken);
            if (list == null)
            {
                return null;
            }

            list.Items = await db.UserListItems.AsNoTracking()
                .Where(i => i.UserListId == id && (admin || i.Movie.IsVisible))
                .OrderByDescending(i => i.AddedAt)
                .Select(i => new UserListItemResponse
                {
                    MovieId = i.MovieId,
                    Title = i.Movie.Title,
                    PosterUrl = i.Movie.PosterUrl,
                    ReleaseYear = i.Movie.ReleaseYear,
                    Type = i.Movie.Type,
                    AverageRating = i.Movie.Reviews.Where(r => r.Status == Models.Enums.ReviewStatus.Active).Select(r => (double?)r.Rating).Average() ?? 0,
                    AddedAt = i.AddedAt
                })
                .ToListAsync(cancellationToken);
            list.ItemCount = list.Items.Count;
            list.PosterUrls = list.Items.Take(4).Select(i => i.PosterUrl).ToList();
            return list;
        }

        public async Task<UserListSummaryResponse> CreateAsync(UserListRequest request, CancellationToken cancellationToken = default)
        {
            var userId = identity.GetCurrentUserId();
            var name = request.Name.Trim();
            if (name.Length == 0)
            {
                throw new UserException("Naziv liste je obavezan.");
            }

            if (await db.UserLists.CountAsync(l => l.UserId == userId, cancellationToken) >= MaxListsPerUser)
            {
                throw new UserException($"Možete imati najviše {MaxListsPerUser} lista.");
            }

            if (await db.UserLists.AnyAsync(l => l.UserId == userId && l.Name == name, cancellationToken))
            {
                throw new UserException($"Lista '{name}' već postoji.");
            }

            var list = new UserList { UserId = userId, Name = name };
            db.UserLists.Add(list);
            await db.SaveChangesAsync(cancellationToken);
            return new UserListSummaryResponse { Id = list.Id, UserId = userId, Name = list.Name, CreatedAt = list.CreatedAt };
        }

        private async Task<UserList> OwnedAsync(int id, CancellationToken cancellationToken)
        {
            var list = await db.UserLists.FirstOrDefaultAsync(l => l.Id == id, cancellationToken)
                ?? throw new NotFoundException("Lista", id);
            if (list.UserId != identity.GetCurrentUserId())
            {
                throw new UnauthorizedAccessException("Možete mijenjati samo vlastite liste.");
            }

            return list;
        }

        public async Task<UserListSummaryResponse?> RenameAsync(int id, UserListRequest request, CancellationToken cancellationToken = default)
        {
            var list = await OwnedAsync(id, cancellationToken);
            var name = request.Name.Trim();
            if (name.Length == 0)
            {
                throw new UserException("Naziv liste je obavezan.");
            }

            if (await db.UserLists.AnyAsync(l => l.UserId == list.UserId && l.Name == name && l.Id != id, cancellationToken))
            {
                throw new UserException($"Lista '{name}' već postoji.");
            }

            list.Name = name;
            await db.SaveChangesAsync(cancellationToken);
            return new UserListSummaryResponse { Id = list.Id, UserId = list.UserId, Name = list.Name, CreatedAt = list.CreatedAt };
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var exists = await db.UserLists.AnyAsync(l => l.Id == id, cancellationToken);
            if (!exists)
            {
                return false;
            }

            var list = await OwnedAsync(id, cancellationToken);
            await using var transaction = db.Database.CurrentTransaction == null ? await db.Database.BeginTransactionAsync(cancellationToken) : null;
            await db.UserListItems.Where(i => i.UserListId == id).ExecuteDeleteAsync(cancellationToken);
            db.UserLists.Remove(list);
            await db.SaveChangesAsync(cancellationToken);
            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return true;
        }

        public async Task<UserListDetailResponse> AddItemAsync(int id, int movieId, CancellationToken cancellationToken = default)
        {
            var list = await OwnedAsync(id, cancellationToken);
            var movie = await db.Movies.AsNoTracking().FirstOrDefaultAsync(m => m.Id == movieId && (m.IsVisible || identity.IsAdmin()), cancellationToken)
                ?? throw new NotFoundException("Film", movieId);

            if (!await db.UserListItems.AnyAsync(i => i.UserListId == id && i.MovieId == movie.Id, cancellationToken))
            {
                if (await db.UserListItems.CountAsync(i => i.UserListId == id, cancellationToken) >= MaxItemsPerList)
                {
                    throw new UserException($"Lista može imati najviše {MaxItemsPerList} naslova.");
                }

                db.UserListItems.Add(new UserListItem { UserListId = list.Id, MovieId = movie.Id });
                try
                {
                    await db.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException)
                {
                    db.ChangeTracker.Clear();
                }
            }

            return (await GetAsync(id, cancellationToken))!;
        }

        public async Task<UserListDetailResponse> RemoveItemAsync(int id, int movieId, CancellationToken cancellationToken = default)
        {
            await OwnedAsync(id, cancellationToken);
            await db.UserListItems.Where(i => i.UserListId == id && i.MovieId == movieId).ExecuteDeleteAsync(cancellationToken);
            return (await GetAsync(id, cancellationToken))!;
        }
    }
}
