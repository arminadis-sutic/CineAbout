using CineAboutApp.Services.Database;
using Microsoft.EntityFrameworkCore;

namespace CineAboutApp.Services.General
{
    internal static class DemoDataCorrections
    {
        public const string ProfileImageUrlsKey = "ProfileImageUrls-v1";
        public const string PlaceholderFilesKey = "PlaceholderFiles-v2";
        public const string DemoRemindersKey = "DemoReminders-v1";
        public const string StreaksKey = "StreaksFromSessions-v1";
        public const string DemoListsKey = "DemoLists-v1";

        private static readonly string[] DemoUsernames =
        {
            "desktop", "mobile", "ana.maric", "marko.juric", "ivana.kovac", "petar.horvat", "marija.babic", "ivan.novak", "lucija.pavic", "tomislav.knezevic"
        };

        public static async Task ConvertProfileImageUrlsAsync(ApplicationDbContext db, CancellationToken ct)
        {
            var profiles = await db.UserProfiles
                .Where(p => p.ProfileImageUrl != null && p.ProfileImageUrl.StartsWith(ProfileImages.Folder + "/"))
                .ToListAsync(ct);
            foreach (var profile in profiles)
            {
                var file = ProfileImages.FileNameOf(profile.ProfileImageUrl);
                if (file != null) profile.ProfileImageUrl = ProfileImages.ToUrl(profile.UserId, file);
            }
            await db.SaveChangesAsync(ct);
        }

        public static async Task FixPlaceholderExtensionsAsync(ApplicationDbContext db, string webRootPath, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(webRootPath)) return;

            string? Convert(string? url)
            {
                if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("/media/", StringComparison.Ordinal) || !url.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
                    return null;
                var relative = url["/media/".Length..];
                if (relative.Contains("..", StringComparison.Ordinal)) return null;
                var oldPath = Path.Combine(webRootPath, relative.Replace('/', Path.DirectorySeparatorChar));
                var newPath = Path.ChangeExtension(oldPath, ".png");
                if (File.Exists(oldPath))
                {
                    if (!IsPng(oldPath)) return null;
                    if (!File.Exists(newPath)) File.Copy(oldPath, newPath);
                }
                else if (!File.Exists(newPath))
                {
                    var (width, height) = relative.StartsWith("studios/", StringComparison.Ordinal) ? (400, 200)
                        : relative.StartsWith("posters/", StringComparison.Ordinal) ? (300, 450) : (300, 300);
                    Directory.CreateDirectory(Path.GetDirectoryName(newPath)!);
                    File.WriteAllBytes(newPath, DataSeeder.CreateSolidPng(width, height, 58, 17, 20));
                }
                return Path.ChangeExtension(url, ".png");
            }

            foreach (var studio in await db.Studios.Where(s => s.LogoUrl.StartsWith("/media/")).ToListAsync(ct))
                studio.LogoUrl = Convert(studio.LogoUrl) ?? studio.LogoUrl;
            foreach (var person in await db.Persons.Where(p => p.PhotoUrl.StartsWith("/media/")).ToListAsync(ct))
                person.PhotoUrl = Convert(person.PhotoUrl) ?? person.PhotoUrl;
            foreach (var movie in await db.Movies.Where(m => m.PosterUrl.StartsWith("/media/")).ToListAsync(ct))
                movie.PosterUrl = Convert(movie.PosterUrl) ?? movie.PosterUrl;
            await db.SaveChangesAsync(ct);
        }

        private static bool IsPng(string path)
        {
            var signature = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            using var stream = File.OpenRead(path);
            var head = new byte[8];
            return stream.Read(head, 0, 8) == 8 && head.SequenceEqual(signature);
        }

        public static async Task RemoveInventedRemindersAsync(ApplicationDbContext db, CancellationToken ct)
        {
            var movieTitles = await db.Movies.AsNoTracking().ToDictionaryAsync(m => m.Id, m => m.Title, ct);
            var invented = await db.Reminders
                .Where(r => r.TmdbId > 900000 && r.TmdbId < 910000)
                .ToListAsync(ct);
            db.Reminders.RemoveRange(invented.Where(r => movieTitles.TryGetValue(r.TmdbId - 900000, out var title) && title == r.Title));
            await db.SaveChangesAsync(ct);
        }

        public static async Task AlignStreaksWithSessionsAsync(ApplicationDbContext db, CancellationToken ct)
        {
            var users = await db.MyAppUsers.Where(u => DemoUsernames.Contains(u.Username)).Select(u => u.Id).ToListAsync(ct);
            foreach (var userId in users)
            {
                var days = (await db.QuizSessions.AsNoTracking()
                        .Where(s => s.UserId == userId && s.IsCompleted && s.Mode == Models.Enums.QuizMode.Daily)
                        .Select(s => s.PlayedAt)
                        .ToListAsync(ct))
                    .Select(d => d.Date).Distinct().OrderBy(d => d).ToList();

                int current = 0, longest = 0, run = 0;
                DateTime? previous = null;
                foreach (var day in days)
                {
                    run = previous.HasValue && previous.Value.AddDays(1) == day ? run + 1 : 1;
                    longest = Math.Max(longest, run);
                    previous = day;
                }
                if (days.Count > 0) current = run;

                var streak = await db.Streaks.FirstOrDefaultAsync(s => s.UserId == userId, ct);
                if (days.Count == 0)
                {
                    if (streak != null) { streak.CurrentStreak = 0; streak.LongestStreak = 0; }
                    continue;
                }

                if (streak == null)
                    db.Streaks.Add(new Streak { UserId = userId, CurrentStreak = current, LongestStreak = longest, LastPlayedDate = days[^1] });
                else
                {
                    streak.CurrentStreak = current;
                    streak.LongestStreak = longest;
                    streak.LastPlayedDate = days[^1];
                }
            }
            await db.SaveChangesAsync(ct);
        }

        public static async Task AddDemoListsAsync(ApplicationDbContext db, CancellationToken ct)
        {
            var definitions = new (string Username, string ListName, string Genre)[]
            {
                ("ana.maric", "Najbolje drame", "Drama"),
                ("marko.juric", "Akcija za vikend", "Akcija"),
                ("ivana.kovac", "Sci-Fi favoriti", "Sci-Fi"),
                ("petar.horvat", "Krimi večer", "Krimi"),
            };

            foreach (var (username, listName, genreName) in definitions)
            {
                var user = await db.MyAppUsers.FirstOrDefaultAsync(u => u.Username == username && !u.IsDeleted, ct);
                var genre = await db.Genres.FirstOrDefaultAsync(g => g.Name == genreName, ct);
                if (user == null || genre == null) continue;
                if (await db.UserLists.AnyAsync(l => l.UserId == user.Id && l.Name == listName, ct)) continue;

                var movieIds = await db.MovieGenres
                    .Where(mg => mg.GenreId == genre.Id && mg.Movie.IsVisible)
                    .OrderBy(mg => mg.MovieId)
                    .Select(mg => mg.MovieId)
                    .Take(5)
                    .ToListAsync(ct);
                if (movieIds.Count == 0) continue;

                var list = new UserList { UserId = user.Id, Name = listName };
                foreach (var movieId in movieIds) list.Items.Add(new UserListItem { MovieId = movieId });
                db.UserLists.Add(list);
            }
            await db.SaveChangesAsync(ct);
        }
    }
}
