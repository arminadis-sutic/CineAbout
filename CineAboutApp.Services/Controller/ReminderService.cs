using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Interfaces.General;
using CineAboutApp.Model;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;
using CineAboutApp.Services.Database;
using Microsoft.EntityFrameworkCore;

namespace CineAboutApp.Services.Controller
{
    public class ReminderService(
        ApplicationDbContext db,
        IIdentityService identity,
        ITmdbClient tmdb,
        IUserNotifier notifier) : IReminderService
    {
        public async Task<List<UpcomingTitleResponse>> GetUpcomingAsync(CancellationToken cancellationToken = default)
        {
            var userId = identity.GetCurrentUserId();
            var today = DateTime.UtcNow.Date;

            var upcoming = (await tmdb.GetUpcomingAsync(cancellationToken))
                .Where(t => t.ReleaseDate.Date >= today)
                .ToList();

            var reminded = (await db.Reminders.AsNoTracking()
                    .Where(r => r.UserId == userId)
                    .Select(r => r.TmdbId)
                    .ToListAsync(cancellationToken))
                .ToHashSet();

            return upcoming.Select(t => new UpcomingTitleResponse
            {
                TmdbId = t.Id,
                Title = t.Title,
                Overview = t.Overview,
                PosterUrl = t.PosterUrl,
                ReleaseDate = t.ReleaseDate,
                DaysUntilRelease = (t.ReleaseDate.Date - today).Days,
                HasReminder = reminded.Contains(t.Id)
            }).ToList();
        }

        public async Task<MyPagedList<ReminderResponse>> GetMineAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var userId = identity.GetCurrentUserId();
            pageNumber = Math.Max(pageNumber, 1);
            pageSize = Math.Clamp(pageSize, 1, BaseSearchObject.MaxPageSize);

            var query = db.Reminders.AsNoTracking().Where(r => r.UserId == userId);
            var total = await query.CountAsync(cancellationToken);
            var reminders = await query
                .OrderBy(r => r.ReleaseDate).ThenBy(r => r.Id)
                .Skip((pageNumber - 1) * pageSize).Take(pageSize)
                .ToListAsync(cancellationToken);

            return new MyPagedList<ReminderResponse>(reminders.Select(ToResponse).ToList(), total, pageNumber, pageSize);
        }

        public async Task<ReminderResponse> CreateAsync(int tmdbId, CancellationToken cancellationToken = default)
        {
            var userId = identity.GetCurrentUserId();

            if (await db.Reminders.AnyAsync(r => r.UserId == userId && r.TmdbId == tmdbId, cancellationToken))
                throw new BusinessException("Podsjetnik za ovaj naslov već postoji.");

            var title = await tmdb.GetTitleAsync(tmdbId, cancellationToken)
                ?? throw new NotFoundException("Naslov nije pronađen u TMDB bazi.");

            if (title.ReleaseDate.Date < DateTime.UtcNow.Date)
                throw new BusinessException("Ovaj naslov je već izašao. Podsjetnik je moguće postaviti samo za nadolazeće naslove.");

            var reminder = new Reminder
            {
                UserId = userId,
                TmdbId = title.Id,
                Title = title.Title,
                PosterUrl = title.PosterUrl,
                ReleaseDate = title.ReleaseDate.Date,
                NotificationSent = false
            };
            db.Reminders.Add(reminder);
            await db.SaveChangesAsync(cancellationToken);

            return ToResponse(reminder);
        }

        public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var userId = identity.GetCurrentUserId();
            var removed = await db.Reminders
                .Where(r => r.Id == id && r.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);

            if (removed == 0)
                throw new NotFoundException("Podsjetnik", id);
        }

        public async Task<int> DispatchDueAsync(CancellationToken cancellationToken = default)
        {
            var today = DateTime.UtcNow.Date;
            var due = await db.Reminders
                .Where(r => !r.NotificationSent && r.ReleaseDate <= today)
                .ToListAsync(cancellationToken);

            foreach (var reminder in due)
            {
                if (reminder.ReleaseDate.Date < today)
                {
                    reminder.NotificationSent = true;
                    continue;
                }

                await notifier.NotifyAsync(
                    [reminder.UserId],
                    "Izlazi danas!",
                    $"'{reminder.Title}' izlazi danas. Uživajte u gledanju!",
                    new Dictionary<string, string> { ["type"] = "reminder", ["tmdbId"] = reminder.TmdbId.ToString() },
                    cancellationToken);

                reminder.NotificationSent = true;
            }

            if (due.Count > 0)
                await db.SaveChangesAsync(cancellationToken);

            return due.Count(r => r.ReleaseDate.Date == today);
        }

        private static ReminderResponse ToResponse(Reminder r) => new()
        {
            Id = r.Id,
            TmdbId = r.TmdbId,
            Title = r.Title,
            PosterUrl = r.PosterUrl,
            ReleaseDate = r.ReleaseDate,
            DaysUntilRelease = Math.Max((r.ReleaseDate.Date - DateTime.UtcNow.Date).Days, 0),
            NotificationSent = r.NotificationSent
        };
    }
}
