using CineAboutApp.Interfaces.General;
using CineAboutApp.Models;
using CineAboutApp.Models.Enums;
using CineAboutApp.Services.Database;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Text;
using ThreadEntity = CineAboutApp.Services.Database.Thread;

namespace CineAboutApp.Services.General
{
    public static class DataSeeder
    {
        private static readonly (byte R, byte G, byte B)[] Palette =
        {
            (188, 60, 60), (60, 140, 60), (60, 90, 180), (200, 160, 50),
            (140, 70, 160), (60, 150, 150), (200, 100, 60), (100, 100, 140),
            (160, 60, 120), (80, 120, 160), (160, 160, 60), (60, 160, 120),
        };

        public const string InitialCatalogKey = "Seed.InitialCatalog.v1";
        public const string CatalogEnrichmentKey = "Seed.CatalogEnrichment.v1";

        public static async Task SeedAsync(
            ApplicationDbContext db,
            IPasswordHasher passwordHasher,
            string webRootPath,
            CancellationToken cancellationToken = default)
        {
            await RunOnceAsync(db, InitialCatalogKey,
                () => SeedInitialCatalogAsync(db, passwordHasher, webRootPath, cancellationToken), cancellationToken);
            await RunOnceAsync(db, CatalogEnrichmentKey,
                () => CatalogSeeder.ApplyAsync(db, cancellationToken), cancellationToken);

            await RunOnceAsync(db, DemoDataCorrections.ProfileImageUrlsKey,
                () => DemoDataCorrections.ConvertProfileImageUrlsAsync(db, cancellationToken), cancellationToken);
            await RunOnceAsync(db, DemoDataCorrections.PlaceholderFilesKey,
                () => DemoDataCorrections.FixPlaceholderExtensionsAsync(db, webRootPath, cancellationToken), cancellationToken);
            await RunOnceAsync(db, DemoDataCorrections.DemoRemindersKey,
                () => DemoDataCorrections.RemoveInventedRemindersAsync(db, cancellationToken), cancellationToken);
            await RunOnceAsync(db, DemoDataCorrections.StreaksKey,
                () => DemoDataCorrections.AlignStreaksWithSessionsAsync(db, cancellationToken), cancellationToken);
            await RunOnceAsync(db, DemoDataCorrections.DemoListsKey,
                () => DemoDataCorrections.AddDemoListsAsync(db, cancellationToken), cancellationToken);
        }

        private static async Task RunOnceAsync(ApplicationDbContext db, string key, Func<Task> work, CancellationToken cancellationToken)
        {
            if (await db.SeedHistories.AnyAsync(s => s.Key == key, cancellationToken))
            {
                return;
            }

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            await work();
            db.SeedHistories.Add(new SeedHistory { Key = key, AppliedAt = DateTime.UtcNow });
            try
            {
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                db.ChangeTracker.Clear();
                if (!await db.SeedHistories.AsNoTracking().AnyAsync(s => s.Key == key, cancellationToken))
                {
                    throw;
                }
            }
        }

        private static async Task SeedInitialCatalogAsync(
            ApplicationDbContext db,
            IPasswordHasher passwordHasher,
            string webRootPath,
            CancellationToken cancellationToken)
        {
            var rng = new Random(20240101);
            var now = DateTime.UtcNow;

            if (!await db.AppSettings.AnyAsync(cancellationToken))
            {
                db.AppSettings.Add(new AppSettings { AutoActivateNewUsers = true });
                await db.SaveChangesAsync(cancellationToken);
            }

            if (!await db.Roles.AnyAsync(cancellationToken))
            {
                db.Roles.Add(new Role { Name = AppRoles.Admin });
                db.Roles.Add(new Role { Name = AppRoles.User });
                await db.SaveChangesAsync(cancellationToken);
            }
            var roles = await db.Roles.ToDictionaryAsync(r => r.Name, cancellationToken);
            var adminRole = roles[AppRoles.Admin];
            var userRole = roles[AppRoles.User];

            var countries = await SeedCountriesAsync(db, cancellationToken);
            var genres = await SeedGenresAsync(db, cancellationToken);
            var languages = await SeedLanguagesAsync(db, cancellationToken);
            var awardTypes = await SeedAwardTypesAsync(db, cancellationToken);

            var studios = await SeedStudiosAsync(db, countries, webRootPath, cancellationToken);
            var actors = await SeedPersonsAsync(db, webRootPath, cancellationToken);
            var movies = await SeedMoviesAsync(db, languages, studios, webRootPath, cancellationToken);
            await SeedMovieGenresAsync(db, movies, genres, rng, cancellationToken);
            await SeedMovieCastsAsync(db, movies, actors, rng, cancellationToken);
            await SeedAwardsAsync(db, movies, actors, awardTypes, cancellationToken);

            var users = await SeedUsersAsync(db, passwordHasher, countries, rng, cancellationToken);
            await SeedUserRolesAsync(db, users, adminRole, userRole, cancellationToken);
            var cities = await SeedCitiesAsync(db, countries, cancellationToken);
            await SeedUserProfilesAsync(db, users, cities, webRootPath, cancellationToken);
            await SeedStreaksAsync(db, users, rng, cancellationToken);

            var reviews = await SeedReviewsAsync(db, users, movies, rng, cancellationToken);
            var threads = await SeedThreadsAsync(db, users, rng, cancellationToken);
            var comments = await SeedCommentsAsync(db, users, threads, rng, cancellationToken);
            await SeedVotesAsync(db, users, reviews, threads, comments, rng, cancellationToken);
            await SeedWatchlistAsync(db, users, movies, rng, cancellationToken);
            await SeedSwipeHistoryAsync(db, users, movies, rng, cancellationToken);
            await SeedFollowsAsync(db, users, rng, cancellationToken);
            await SeedReportsAsync(db, users, reviews, threads, comments, rng, cancellationToken);
            await SeedThreadTagsAsync(db, threads, movies, cancellationToken);
            await SeedGenrePreferencesAsync(db, users, genres, rng, cancellationToken);

            var quizQuestions = await SeedQuizQuestionsAsync(db, cancellationToken);
            await SeedQuizSessionsAsync(db, users, quizQuestions, rng, cancellationToken);

            await SeedNotificationsAsync(db, users, rng, cancellationToken);

            await db.SaveChangesAsync(cancellationToken);
        }

        private static async Task<Dictionary<string, Country>> SeedCountriesAsync(ApplicationDbContext db, CancellationToken ct)
        {
            if (!await db.Countries.AnyAsync(ct))
            {
                string[] names =
                {
                    "Bosna i Hercegovina", "Hrvatska", "Srbija", "Slovenija",
                    "Njemačka", "Austrija", "Italija", "Francuska",
                    "Ujedinjeno Kraljevstvo", "SAD", "Španija", "Švicarska"
                };
                db.Countries.AddRange(names.Select(n => new Country { Name = n }));
                await db.SaveChangesAsync(ct);
            }
            return await db.Countries.OrderBy(c => c.Id).ToFirstWinsDictionaryAsync(c => c.Name, ct);
        }

        private static async Task<Dictionary<string, Genre>> SeedGenresAsync(ApplicationDbContext db, CancellationToken ct)
        {
            if (!await db.Genres.AnyAsync(ct))
            {
                string[] names =
                {
                    "Akcija", "Drama", "Komedija", "Triler", "Horor", "Sci-Fi",
                    "Romantika", "Dokumentarni", "Animacija", "Krimi", "Avantura", "Fantazija", "Misterija"
                };
                db.Genres.AddRange(names.Select(n => new Genre { Name = n }));
                await db.SaveChangesAsync(ct);
            }
            return await db.Genres.OrderBy(g => g.Id).ToFirstWinsDictionaryAsync(g => g.Name, ct);
        }

        private static async Task<Dictionary<string, Language>> SeedLanguagesAsync(ApplicationDbContext db, CancellationToken ct)
        {
            if (!await db.Languages.AnyAsync(ct))
            {
                string[] names = { "English", "Bosanski", "Hrvatski", "Srpski", "Njemački", "Francuski", "Španski" };
                db.Languages.AddRange(names.Select(n => new Language { Name = n }));
                await db.SaveChangesAsync(ct);
            }
            return await db.Languages.OrderBy(l => l.Id).ToFirstWinsDictionaryAsync(l => l.Name, ct);
        }

        private static async Task<Dictionary<string, AwardType>> SeedAwardTypesAsync(ApplicationDbContext db, CancellationToken ct)
        {
            if (!await db.AwardTypes.AnyAsync(ct))
            {
                string[] names = { "Oscar", "Zlatni globus", "BAFTA", "Emmy", "Cannes", "SAG" };
                db.AwardTypes.AddRange(names.Select(n => new AwardType { Name = n }));
                await db.SaveChangesAsync(ct);
            }
            return await db.AwardTypes.OrderBy(a => a.Id).ToFirstWinsDictionaryAsync(a => a.Name, ct);
        }

        private static async Task<Dictionary<string, Studio>> SeedStudiosAsync(
            ApplicationDbContext db, Dictionary<string, Country> countries, string webRootPath, CancellationToken ct)
        {
            if (!await db.Studios.AnyAsync(ct))
            {
                var defs = new (string Name, string Country, int Year)[]
                {
                    ("Warner Bros.", "SAD", 1923),
                    ("Universal", "SAD", 1912),
                    ("Paramount", "SAD", 1912),
                    ("Sony", "SAD", 1987),
                    ("20th Century", "SAD", 1935),
                    ("Lionsgate", "SAD", 1997),
                    ("A24", "SAD", 2012),
                };
                int i = 0;
                foreach (var d in defs)
                {
                    db.Studios.Add(new Studio
                    {
                        Name = d.Name,
                        CountryId = countries[d.Country].Id,
                        FoundedYear = d.Year,
                        Description = $"{d.Name} je poznati filmski studio.",
                        LogoUrl = $"/media/studios/{Slug(d.Name)}.png"
                    });
                    WritePlaceholder(webRootPath, $"studios/{Slug(d.Name)}.png", 400, 200, Palette[i++ % Palette.Length]);
                }
                await db.SaveChangesAsync(ct);
            }
            return await db.Studios.OrderBy(s => s.Id).ToFirstWinsDictionaryAsync(s => s.Name, ct);
        }

        private static async Task<List<Person>> SeedPersonsAsync(ApplicationDbContext db, string webRootPath, CancellationToken ct)
        {
            if (!await db.Persons.AnyAsync(ct))
            {
                var defs = new (string Name, PersonRoleType Role, int Year)[]
                {
                    ("Christopher Nolan", PersonRoleType.Director, 1970),
                    ("Quentin Tarantino", PersonRoleType.Director, 1963),
                    ("Steven Spielberg", PersonRoleType.Director, 1946),
                    ("Martin Scorsese", PersonRoleType.Director, 1942),
                    ("Denis Villeneuve", PersonRoleType.Director, 1967),
                    ("Greta Gerwig", PersonRoleType.Director, 1983),
                    ("David Fincher", PersonRoleType.Director, 1962),
                    ("Leonardo DiCaprio", PersonRoleType.Actor, 1974),
                    ("Brad Pitt", PersonRoleType.Actor, 1963),
                    ("Margot Robbie", PersonRoleType.Actor, 1990),
                    ("Robert De Niro", PersonRoleType.Actor, 1943),
                    ("Meryl Streep", PersonRoleType.Actor, 1949),
                    ("Cate Blanchett", PersonRoleType.Actor, 1969),
                    ("Tom Hanks", PersonRoleType.Actor, 1956),
                    ("Scarlett Johansson", PersonRoleType.Actor, 1984),
                    ("Joaquin Phoenix", PersonRoleType.Actor, 1974),
                    ("Emma Stone", PersonRoleType.Actor, 1988),
                    ("Ryan Gosling", PersonRoleType.Actor, 1980),
                    ("Christian Bale", PersonRoleType.Actor, 1974),
                    ("Natalie Portman", PersonRoleType.Actor, 1981),
                    ("Timothée Chalamet", PersonRoleType.Actor, 1995),
                    ("Zendaya", PersonRoleType.Actor, 1996),
                };
                int i = 0;
                foreach (var d in defs)
                {
                    db.Persons.Add(new Person
                    {
                        FullName = d.Name,
                        RoleType = d.Role,
                        BirthDate = new DateTime(d.Year, rngMonth(d.Year), rngDay(d.Year)),
                        Biography = $"{d.Name} je poznati filmski {(d.Role == PersonRoleType.Actor ? "glumac" : "režiser")}.",
                        PhotoUrl = $"/media/persons/{Slug(d.Name)}.png"
                    });
                    WritePlaceholder(webRootPath, $"persons/{Slug(d.Name)}.png", 300, 300, Palette[i++ % Palette.Length]);
                }
                await db.SaveChangesAsync(ct);
            }

            var persons = await db.Persons.ToListAsync(ct);
            return persons.Where(p => p.RoleType == PersonRoleType.Actor).ToList();
        }

        private static async Task<List<Movie>> SeedMoviesAsync(
            ApplicationDbContext db,
            Dictionary<string, Language> languages,
            Dictionary<string, Studio> studios,
            string webRootPath,
            CancellationToken ct)
        {
            if (!await db.Movies.AnyAsync(ct))
            {
                var defs = new (string Title, MovieType Type, int Year, int Duration, string Language, string Studio)[]
                {
                    ("Inception", MovieType.Movie, 2010, 148, "English", "Warner Bros."),
                    ("The Dark Knight", MovieType.Movie, 2008, 152, "English", "Warner Bros."),
                    ("Pulp Fiction", MovieType.Movie, 1994, 154, "English", "Universal"),
                    ("Titanic", MovieType.Movie, 1997, 195, "English", "Paramount"),
                    ("Avatar", MovieType.Movie, 2009, 162, "English", "20th Century"),
                    ("The Godfather", MovieType.Movie, 1972, 175, "English", "Paramount"),
                    ("Interstellar", MovieType.Movie, 2014, 169, "English", "Warner Bros."),
                    ("Fight Club", MovieType.Movie, 1999, 139, "English", "20th Century"),
                    ("The Matrix", MovieType.Movie, 1999, 136, "English", "Warner Bros."),
                    ("Gladiator", MovieType.Movie, 2000, 155, "English", "Universal"),
                    ("Parasite", MovieType.Movie, 2019, 132, "English", "A24"),
                    ("La La Land", MovieType.Movie, 2016, 128, "English", "Lionsgate"),
                    ("Dune", MovieType.Movie, 2021, 155, "English", "Warner Bros."),
                    ("Joker", MovieType.Movie, 2019, 122, "English", "Warner Bros."),
                    ("The Wolf of Wall Street", MovieType.Movie, 2013, 180, "English", "Paramount"),
                    ("Django Unchained", MovieType.Movie, 2012, 165, "English", "Sony"),
                    ("Once Upon a Time in Hollywood", MovieType.Movie, 2019, 161, "English", "Sony"),
                    ("Goodfellas", MovieType.Movie, 1990, 145, "English", "Warner Bros."),
                    ("The Shawshank Redemption", MovieType.Movie, 1994, 142, "English", "Sony"),
                    ("Forrest Gump", MovieType.Movie, 1994, 142, "English", "Paramount"),
                    ("Spirited Away", MovieType.Movie, 2001, 125, "English", "Sony"),
                    ("Amélie", MovieType.Movie, 2001, 122, "Francuski", "A24"),
                    ("Pan's Labyrinth", MovieType.Movie, 2006, 118, "Španski", "Universal"),
                    ("Breaking Bad", MovieType.Series, 2008, 47, "English", "Sony"),
                    ("Stranger Things", MovieType.Series, 2016, 50, "English", "Universal"),
                    ("The Crown", MovieType.Series, 2016, 58, "English", "Sony"),
                    ("Game of Thrones", MovieType.Series, 2011, 57, "English", "Warner Bros."),
                    ("Chernobyl", MovieType.Series, 2019, 60, "English", "Sony"),
                    ("Dark", MovieType.Series, 2017, 53, "Njemački", "A24"),
                };
                int i = 0;
                foreach (var d in defs)
                {
                    db.Movies.Add(new Movie
                    {
                        Title = d.Title,
                        OriginalTitle = d.Title,
                        Type = d.Type,
                        ReleaseYear = d.Year,
                        Duration = d.Duration,
                        Synopsis = $"{d.Title} je filmska priča sa vrhunskom glumačkom ekipom i nezaboravnom pričom.",
                        PosterUrl = $"/media/posters/{Slug(d.Title)}.png",
                        IsVisible = true,
                        LanguageId = languages[d.Language].Id,
                        StudioId = studios[d.Studio].Id,
                    });
                    WritePlaceholder(webRootPath, $"posters/{Slug(d.Title)}.png", 300, 450, Palette[i++ % Palette.Length]);
                }
                await db.SaveChangesAsync(ct);
            }
            return await db.Movies.ToListAsync(ct);
        }

        private static async Task SeedMovieGenresAsync(
            ApplicationDbContext db, List<Movie> movies, Dictionary<string, Genre> genres, Random rng, CancellationToken ct)
        {
            if (!await db.MovieGenres.AnyAsync(ct))
            {
                var genreList = genres.Values.ToList();
                foreach (var movie in movies)
                {
                    var count = rng.Next(2, 4);
                    var picked = genreList.OrderBy(_ => rng.Next()).Take(count).ToList();
                    foreach (var g in picked)
                        db.MovieGenres.Add(new MovieGenre { MovieId = movie.Id, GenreId = g.Id });
                }
                await db.SaveChangesAsync(ct);
            }
        }

        private static async Task SeedMovieCastsAsync(
            ApplicationDbContext db, List<Movie> movies, List<Person> actors, Random rng, CancellationToken ct)
        {
            if (!await db.MovieCasts.AnyAsync(ct))
            {
                string[] roles = { "Glavna uloga", "Sporedna uloga", "Antagonist", "Gost" };
                foreach (var movie in movies)
                {
                    var count = rng.Next(2, 5);
                    var picked = actors.OrderBy(_ => rng.Next()).Take(count).ToList();
                    foreach (var actor in picked)
                        db.MovieCasts.Add(new MovieCast
                        {
                            MovieId = movie.Id,
                            PersonId = actor.Id,
                            RoleInMovie = roles[rng.Next(roles.Length)]
                        });
                }
                await db.SaveChangesAsync(ct);
            }
        }

        private static async Task SeedAwardsAsync(
            ApplicationDbContext db,
            List<Movie> movies,
            List<Person> actors,
            Dictionary<string, AwardType> awardTypes,
            CancellationToken ct)
        {
            if (!await db.Awards.AnyAsync(ct))
            {
                var movieAwards = new (string Type, string Movie, int Year, string Category)[]
                {
                    ("Oscar", "Inception", 2011, "Najbolji film"),
                    ("Oscar", "The Dark Knight", 2009, "Najbolja sporedna uloga"),
                    ("BAFTA", "Dune", 2022, "Najbolji zvuk"),
                    ("Cannes", "Pulp Fiction", 1994, "Zlatna palma"),
                    ("Zlatni globus", "La La Land", 2017, "Najbolji mjuzikl"),
                };
                var personAwards = new (string Type, string Person, int Year, string Category)[]
                {
                    ("Oscar", "Leonardo DiCaprio", 2016, "Najbolji glumac"),
                    ("Oscar", "Meryl Streep", 2012, "Najbolja glumica"),
                    ("SAG", "Robert De Niro", 2003, "Najbolji glumac"),
                };

                var movieById = movies.GroupBy(m => m.Title).ToDictionary(g => g.Key, g => g.First());
                var actorByName = actors.GroupBy(a => a.FullName).ToDictionary(g => g.Key, g => g.First());

                foreach (var a in movieAwards)
                    db.Awards.Add(new Award
                    {
                        AwardTypeId = awardTypes[a.Type].Id,
                        MovieId = movieById[a.Movie].Id,
                        Year = a.Year,
                        Category = a.Category
                    });

                foreach (var a in personAwards)
                    db.Awards.Add(new Award
                    {
                        AwardTypeId = awardTypes[a.Type].Id,
                        PersonId = actorByName[a.Person].Id,
                        Year = a.Year,
                        Category = a.Category
                    });

                await db.SaveChangesAsync(ct);
            }
        }

        private static async Task<List<MyAppUser>> SeedUsersAsync(
            ApplicationDbContext db,
            IPasswordHasher passwordHasher,
            Dictionary<string, Country> countries,
            Random rng,
            CancellationToken ct)
        {
            if (!await db.MyAppUsers.AnyAsync(ct))
            {
                var defs = new (string Username, string First, string Last, string Role, int DaysAgo)[]
                {
                    ("desktop", "Desktop", "Administrator", AppRoles.Admin, 1),
                    ("mobile", "Mobile", "User", AppRoles.User, 2),
                    ("ana.maric", "Ana", "Marić", AppRoles.User, 3),
                    ("marko.juric", "Marko", "Jurić", AppRoles.User, 4),
                    ("ivana.kovac", "Ivana", "Kovač", AppRoles.User, 5),
                    ("petar.horvat", "Petar", "Horvat", AppRoles.User, 6),
                    ("marija.babic", "Marija", "Babić", AppRoles.User, 7),
                    ("ivan.novak", "Ivan", "Novak", AppRoles.User, 9),
                    ("lucija.pavic", "Lucija", "Pavić", AppRoles.User, 12),
                    ("tomislav.knezevic", "Tomislav", "Knežević", AppRoles.User, 15),
                };

                var countryList = countries.Values.ToList();
                foreach (var d in defs)
                {
                    var (hash, salt) = passwordHasher.Hash("test");
                    db.MyAppUsers.Add(new MyAppUser
                    {
                        FirstName = d.First,
                        LastName = d.Last,
                        Email = $"{d.Username}@cineabout.test",
                        PasswordHash = hash,
                        PasswordSalt = salt,
                        Username = d.Username,
                        IsActive = true,
                        RegistrationDate = DateTime.UtcNow.AddDays(-d.DaysAgo),
                        CountryId = countryList[rng.Next(countryList.Count)].Id,
                        LastActivity = DateTime.UtcNow.AddDays(-rng.Next(0, Math.Max(d.DaysAgo, 1)))
                    });
                }
                await db.SaveChangesAsync(ct);
            }
            return await db.MyAppUsers.ToListAsync(ct);
        }

        private static async Task SeedUserRolesAsync(
            ApplicationDbContext db, List<MyAppUser> users, Role adminRole, Role userRole, CancellationToken ct)
        {
            if (!await db.UserRoles.AnyAsync(ct))
            {
                foreach (var user in users)
                {
                    if (user.Username == "desktop")
                        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = adminRole.Id, UpdatedAt = DateTime.UtcNow, IsActive = true });
                    db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = userRole.Id, UpdatedAt = DateTime.UtcNow, IsActive = true });
                }
                await db.SaveChangesAsync(ct);
            }
        }

        private static async Task<Dictionary<string, City>> SeedCitiesAsync(ApplicationDbContext db, Dictionary<string, Country> countries, CancellationToken ct)
        {
            if (!await db.Cities.AnyAsync(ct))
            {
                var defs = new (string City, string Country)[]
                {
                    ("Sarajevo", "Bosna i Hercegovina"), ("Mostar", "Bosna i Hercegovina"), ("Tuzla", "Bosna i Hercegovina"), ("Banja Luka", "Bosna i Hercegovina"),
                    ("Zagreb", "Hrvatska"), ("Split", "Hrvatska"), ("Beograd", "Srbija"), ("Novi Sad", "Srbija"), ("Ljubljana", "Slovenija"),
                };
                foreach (var (city, country) in defs)
                {
                    if (countries.TryGetValue(country, out var c))
                    {
                        db.Cities.Add(new City { Name = city, CountryId = c.Id });
                    }
                }

                await db.SaveChangesAsync(ct);
            }

            return await db.Cities.OrderBy(c => c.Id).ToFirstWinsDictionaryAsync(c => c.Name, ct);
        }

        private static async Task SeedUserProfilesAsync(
            ApplicationDbContext db, List<MyAppUser> users, Dictionary<string, City> cityLookup, string webRootPath, CancellationToken ct)
        {
            if (!await db.UserProfiles.AnyAsync(ct))
            {
                string[] cities = { "Sarajevo", "Zagreb", "Beograd", "Mostar", "Tuzla", "Banja Luka" };
                int i = 0;
                foreach (var user in users)
                {
                    db.UserProfiles.Add(new UserProfile
                    {
                        UserId = user.Id,
                        Bio = $"Ja sam {user.Username} i volim filmove.",
                        ProfileImageUrl = $"/media/profiles/{user.Username}.png",
                        CityId = cityLookup.TryGetValue(cities[i % cities.Length], out var cityRow) ? cityRow.Id : null,
                        IsProfilePublic = true
                    });
                    WritePlaceholder(webRootPath, $"profiles/{user.Username}.png", 200, 200, Palette[i++ % Palette.Length]);
                }
                await db.SaveChangesAsync(ct);
            }
        }

        private static async Task SeedStreaksAsync(ApplicationDbContext db, List<MyAppUser> users, Random rng, CancellationToken ct)
        {
            if (!await db.Streaks.AnyAsync(ct))
            {
                foreach (var user in users)
                {
                    var current = rng.Next(0, 21);
                    db.Streaks.Add(new Streak
                    {
                        UserId = user.Id,
                        CurrentStreak = current,
                        LongestStreak = current + rng.Next(0, 10),
                        LastPlayedDate = DateTime.UtcNow.AddDays(-rng.Next(0, 30))
                    });
                }
                await db.SaveChangesAsync(ct);
            }
        }

        private static async Task<List<Review>> SeedReviewsAsync(
            ApplicationDbContext db, List<MyAppUser> users, List<Movie> movies, Random rng, CancellationToken ct)
        {
            if (!await db.Reviews.AnyAsync(ct))
            {
                string[] texts =
                {
                    "Odličan film, preporučujem svima!",
                    "Solidna režija, ali radnja je predvidljiva.",
                    "Jedan od najboljih filmova koje sam gledao.",
                    "Glumačka postava je vrhunska.",
                    "Film me je ostavio bez riječi.",
                    "Prosječan film, ništa posebno.",
                    "Remek-djelo moderne kinematografije.",
                    "Priča je previše razvučena.",
                    "Vizuelno impresivno, ali scenarij slab.",
                    "Film koji se mora pogledati više puta.",
                };
                ReviewStatus[] statuses = { ReviewStatus.Active, ReviewStatus.Active, ReviewStatus.Active, ReviewStatus.Reported, ReviewStatus.Removed };

                var reviewUsers = users.Where(u => u.Username != "desktop").ToList();
                var reviews = new List<Review>();
                var idx = 0;

                foreach (var movie in movies.Take(6))
                {
                    for (int k = 0; k < 3; k++)
                    {
                        reviews.Add(new Review
                        {
                            UserId = reviewUsers[(idx + k) % reviewUsers.Count].Id,
                            MovieId = movie.Id,
                            Rating = rng.Next(0, 11),
                            Text = texts[idx % texts.Length],
                            CreatedAt = RecentDate(rng),
                            Status = statuses[idx % statuses.Length]
                        });
                        idx++;
                    }
                }

                var seenPairs = reviews.Select(r => (r.UserId, r.MovieId)).ToHashSet();
                while (reviews.Count < 45)
                {
                    var userId = reviewUsers[rng.Next(reviewUsers.Count)].Id;
                    var movieId = movies[rng.Next(movies.Count)].Id;
                    if (!seenPairs.Add((userId, movieId)))
                        continue;

                    reviews.Add(new Review
                    {
                        UserId = userId,
                        MovieId = movieId,
                        Rating = rng.Next(0, 11),
                        Text = texts[idx % texts.Length],
                        CreatedAt = RecentDate(rng),
                        Status = statuses[idx % statuses.Length]
                    });
                    idx++;
                }

                db.Reviews.AddRange(reviews);
                await db.SaveChangesAsync(ct);
            }
            return await db.Reviews.ToListAsync(ct);
        }

        private static async Task<List<ThreadEntity>> SeedThreadsAsync(
            ApplicationDbContext db, List<MyAppUser> users, Random rng, CancellationToken ct)
        {
            if (!await db.Threads.AnyAsync(ct))
            {
                string[] titles =
                {
                    "Najbolji sci-fi filmovi svih vremena",
                    "Šta mislite o kraju filma Inception?",
                    "Koji Tarantinov film vam je najdraži?",
                    "Preporuke za vikend gledanje",
                    "Parasite — analiza i diskusija",
                    "Najbolje serije na streaming platformama",
                    "Kako je Dune promijenio žanr?",
                    "Joker vs The Dark Knight — poređenje",
                    "Skriveni dragulji kinematografije",
                    "Filmovi koje treba pogledati barem jednom",
                    "Spoiler diskusija: Fight Club",
                    "Koji glumac najbolje glumi mafijaša?",
                };
                ThreadStatus[] statuses =
                {
                    ThreadStatus.Active, ThreadStatus.Locked, ThreadStatus.Active, ThreadStatus.Active,
                    ThreadStatus.Removed, ThreadStatus.Active, ThreadStatus.Locked, ThreadStatus.Active,
                    ThreadStatus.Active, ThreadStatus.Removed, ThreadStatus.Active, ThreadStatus.Active
                };

                var threadUsers = users.Where(u => u.Username != "desktop").ToList();
                for (int i = 0; i < titles.Length; i++)
                {
                    db.Threads.Add(new ThreadEntity
                    {
                        UserId = threadUsers[i % threadUsers.Count].Id,
                        Title = titles[i],
                        Content = $"Diskusija: {titles[i]}. Podijelite svoje mišljenje u komentarima.",
                        HasSpoiler = i % 3 == 0,
                        Status = statuses[i],
                        CreatedAt = RecentDate(rng)
                    });
                }
                await db.SaveChangesAsync(ct);
            }
            return await db.Threads.ToListAsync(ct);
        }

        private static async Task<List<Comment>> SeedCommentsAsync(
            ApplicationDbContext db, List<MyAppUser> users, List<ThreadEntity> threads, Random rng, CancellationToken ct)
        {
            if (!await db.Comments.AnyAsync(ct))
            {
                string[] contents =
                {
                    "Slažem se u potpunosti!",
                    "Odlična poenta.",
                    "Nisam siguran da se slažem, ali dobra diskusija.",
                    "Ovo je baš ono što sam mislio.",
                    "Hvala na preporuci!",
                    "Film je stvarno bio fenomenalan.",
                    "Moram priznati da sam očekivao više.",
                    "Odlična analiza.",
                    "Gledao sam ga tri puta, svaki put sve bolji.",
                    "Koji je vaš omiljeni dio?",
                    "Definitivno dodajem na listu.",
                    "Odličan thread, nastavi pisati!",
                };
                var commentUsers = users.Where(u => u.Username != "desktop").ToList();
                for (int i = 0; i < 32; i++)
                {
                    db.Comments.Add(new Comment
                    {
                        ThreadId = threads[rng.Next(threads.Count)].Id,
                        UserId = commentUsers[rng.Next(commentUsers.Count)].Id,
                        Content = contents[i % contents.Length],
                        CreatedAt = RecentDate(rng)
                    });
                }
                await db.SaveChangesAsync(ct);
            }
            return await db.Comments.ToListAsync(ct);
        }

        private static async Task SeedVotesAsync(
            ApplicationDbContext db,
            List<MyAppUser> users,
            List<Review> reviews,
            List<ThreadEntity> threads,
            List<Comment> comments,
            Random rng,
            CancellationToken ct)
        {
            var voters = users.Where(u => u.Username != "desktop").ToList();

            if (!await db.ReviewVotes.AnyAsync(ct))
            {
                var seen = new HashSet<(int, int)>();
                for (int i = 0; i < 60 && seen.Count < 60; i++)
                {
                    var review = reviews[rng.Next(reviews.Count)];
                    var user = voters[rng.Next(voters.Count)];
                    if (seen.Add((review.Id, user.Id)))
                        db.ReviewVotes.Add(new ReviewVote { ReviewId = review.Id, UserId = user.Id, IsHelpful = rng.Next(2) == 0 });
                }
            }

            if (!await db.ThreadVotes.AnyAsync(ct))
            {
                var seen = new HashSet<(int, int)>();
                for (int i = 0; i < 50 && seen.Count < 50; i++)
                {
                    var thread = threads[rng.Next(threads.Count)];
                    var user = voters[rng.Next(voters.Count)];
                    if (seen.Add((thread.Id, user.Id)))
                        db.ThreadVotes.Add(new ThreadVote { ThreadId = thread.Id, UserId = user.Id, IsUpvote = rng.Next(2) == 0 });
                }
            }

            if (!await db.CommentVotes.AnyAsync(ct))
            {
                var seen = new HashSet<(int, int)>();
                for (int i = 0; i < 60 && seen.Count < 60; i++)
                {
                    var comment = comments[rng.Next(comments.Count)];
                    var user = voters[rng.Next(voters.Count)];
                    if (seen.Add((comment.Id, user.Id)))
                        db.CommentVotes.Add(new CommentVote { CommentId = comment.Id, UserId = user.Id, IsUpvote = rng.Next(2) == 0 });
                }
            }

            await db.SaveChangesAsync(ct);
        }

        private static async Task SeedWatchlistAsync(
            ApplicationDbContext db, List<MyAppUser> users, List<Movie> movies, Random rng, CancellationToken ct)
        {
            if (!await db.WatchlistItems.AnyAsync(ct))
            {
                var watchUsers = users.Where(u => u.Username != "desktop").ToList();
                var seen = new HashSet<(int, int)>();
                for (int i = 0; i < 32 && seen.Count < 32; i++)
                {
                    var user = watchUsers[rng.Next(watchUsers.Count)];
                    var movie = movies[rng.Next(movies.Count)];
                    if (seen.Add((user.Id, movie.Id)))
                        db.WatchlistItems.Add(new WatchlistItem
                        {
                            UserId = user.Id,
                            MovieId = movie.Id,
                            ListType = rng.Next(2) == 0 ? WatchlistType.Watchlist : WatchlistType.Interested,
                            AddedAt = RecentDate(rng)
                        });
                }
                await db.SaveChangesAsync(ct);
            }
        }

        private static async Task SeedSwipeHistoryAsync(
            ApplicationDbContext db, List<MyAppUser> users, List<Movie> movies, Random rng, CancellationToken ct)
        {
            if (!await db.SwipeHistories.AnyAsync(ct))
            {
                var swipeUsers = users.Where(u => u.Username != "desktop").ToList();
                var seen = new HashSet<(int, int)>();
                int added = 0;
                while (added < 60)
                {
                    var user = swipeUsers[rng.Next(swipeUsers.Count)];
                    var movie = movies[rng.Next(movies.Count)];
                    if (seen.Add((user.Id, movie.Id)))
                    {
                        db.SwipeHistories.Add(new SwipeHistory
                        {
                            UserId = user.Id,
                            MovieId = movie.Id,
                            Direction = rng.Next(2) == 0 ? SwipeDirection.Left : SwipeDirection.Right,
                            SwipedAt = RecentDate(rng)
                        });
                        added++;
                    }
                }
                await db.SaveChangesAsync(ct);
            }
        }

        private static async Task SeedFollowsAsync(
            ApplicationDbContext db, List<MyAppUser> users, Random rng, CancellationToken ct)
        {
            if (!await db.Follows.AnyAsync(ct))
            {
                var seen = new HashSet<(int, int)>();
                for (int i = 0; i < 15 && seen.Count < 15; i++)
                {
                    var follower = users[rng.Next(users.Count)];
                    var following = users[rng.Next(users.Count)];
                    if (follower.Id != following.Id && seen.Add((follower.Id, following.Id)))
                        db.Follows.Add(new Follow
                        {
                            FollowerId = follower.Id,
                            FollowingId = following.Id,
                            CreatedAt = RecentDate(rng)
                        });
                }
                await db.SaveChangesAsync(ct);
            }
        }

        private static async Task SeedReportsAsync(
            ApplicationDbContext db,
            List<MyAppUser> users,
            List<Review> reviews,
            List<ThreadEntity> threads,
            List<Comment> comments,
            Random rng,
            CancellationToken ct)
        {
            if (!await db.Reports.AnyAsync(ct))
            {
                var reporters = users.Where(u => u.Username != "desktop").ToList();
                string[] reasons = { "Spam", "Uvredljiv sadržaj", "Neprikladan jezik", "Lažne informacije" };
                ReportStatus[] statuses = { ReportStatus.Pending, ReportStatus.Resolved, ReportStatus.Dismissed };

                db.Reports.Add(new Report
                {
                    ReporterId = reporters[rng.Next(reporters.Count)].Id,
                    TargetType = ReportTargetType.Review,
                    TargetId = reviews[0].Id,
                    Reason = reasons[0],
                    Status = ReportStatus.Pending,
                    CreatedAt = RecentDate(rng)
                });
                db.Reports.Add(new Report
                {
                    ReporterId = reporters[rng.Next(reporters.Count)].Id,
                    TargetType = ReportTargetType.Thread,
                    TargetId = threads[0].Id,
                    Reason = reasons[1],
                    Status = ReportStatus.Resolved,
                    CreatedAt = RecentDate(rng)
                });
                db.Reports.Add(new Report
                {
                    ReporterId = reporters[rng.Next(reporters.Count)].Id,
                    TargetType = ReportTargetType.Comment,
                    TargetId = comments[0].Id,
                    Reason = reasons[2],
                    Status = ReportStatus.Dismissed,
                    CreatedAt = RecentDate(rng)
                });

                await db.SaveChangesAsync(ct);
            }
        }

        private static async Task<List<QuizQuestion>> SeedQuizQuestionsAsync(ApplicationDbContext db, CancellationToken ct)
        {
            if (!await db.QuizQuestions.AnyAsync(ct))
            {
                if (!await db.QuizCategories.AnyAsync(ct))
                {
                    db.QuizCategories.AddRange(new[] { "redatelji", "glumci", "filmski citati", "godine izdanja", "radnje filmova" }.Select(n => new QuizCategory { Name = n }));
                    await db.SaveChangesAsync(ct);
                }

                var categoryLookup = await db.QuizCategories.OrderBy(c => c.Id).ToFirstWinsDictionaryAsync(c => c.Name, ct);
                var questions = new (string Text, string A, string B, string C, string D, string Correct, string Category, QuizDifficulty Difficulty)[]
                {
                    ("Ko je režirao film Inception?", "Christopher Nolan", "Quentin Tarantino", "Steven Spielberg", "Martin Scorsese", "A", "redatelji", QuizDifficulty.Easy),
                    ("Ko je režirao Pulp Fiction?", "Christopher Nolan", "Quentin Tarantino", "David Fincher", "Greta Gerwig", "B", "redatelji", QuizDifficulty.Medium),
                    ("Ko je režirao Schindlerovu listu?", "Steven Spielberg", "Martin Scorsese", "Denis Villeneuve", "David Fincher", "A", "redatelji", QuizDifficulty.Easy),
                    ("Ko je režirao Dune (2021)?", "Denis Villeneuve", "Christopher Nolan", "Greta Gerwig", "David Fincher", "A", "redatelji", QuizDifficulty.Hard),
                    ("Ko je režirao Once Upon a Time in Hollywood?", "Quentin Tarantino", "Martin Scorsese", "Steven Spielberg", "David Fincher", "A", "redatelji", QuizDifficulty.Hard),
                    ("Ko je režirao La La Land?", "Damien Chazelle", "Greta Gerwig", "David Fincher", "Denis Villeneuve", "A", "redatelji", QuizDifficulty.Hard),
                    ("Ko je glumio Jack Dawsona u Titanicu?", "Leonardo DiCaprio", "Brad Pitt", "Tom Hanks", "Ryan Gosling", "A", "glumci", QuizDifficulty.Easy),
                    ("Koja glumica je glumila Harley Quinn?", "Margot Robbie", "Scarlett Johansson", "Emma Stone", "Natalie Portman", "A", "glumci", QuizDifficulty.Easy),
                    ("Ko je glumio Jokera u filmu Joker (2019)?", "Joaquin Phoenix", "Robert De Niro", "Christian Bale", "Brad Pitt", "A", "glumci", QuizDifficulty.Medium),
                    ("Koji glumac je glumio u La La Land?", "Ryan Gosling", "Timothée Chalamet", "Tom Hanks", "Christian Bale", "A", "glumci", QuizDifficulty.Medium),
                    ("Koja glumica je glumila u Black Swan?", "Natalie Portman", "Emma Stone", "Margot Robbie", "Scarlett Johansson", "A", "glumci", QuizDifficulty.Hard),
                    ("Ko je glumio u Forrest Gump?", "Tom Hanks", "Brad Pitt", "Leonardo DiCaprio", "Robert De Niro", "A", "glumci", QuizDifficulty.Easy),
                    ("Koji glumac je glumio u The Wolf of Wall Street?", "Leonardo DiCaprio", "Brad Pitt", "Ryan Gosling", "Christian Bale", "A", "glumci", QuizDifficulty.Easy),
                    ("Ko je glumio u Goodfellas?", "Robert De Niro", "Tom Hanks", "Joaquin Phoenix", "Brad Pitt", "A", "glumci", QuizDifficulty.Medium),
                    ("Iz kojeg filma je citat 'I'll be back'?", "Terminator", "The Matrix", "Gladiator", "Dune", "A", "filmski citati", QuizDifficulty.Easy),
                    ("Iz kojeg filma je citat 'Here's Johnny!'?", "The Shining", "Fight Club", "Goodfellas", "Joker", "A", "filmski citati", QuizDifficulty.Medium),
                    ("Iz kojeg filma je citat 'May the Force be with you'?", "Star Wars", "Dune", "Avatar", "Interstellar", "A", "filmski citati", QuizDifficulty.Easy),
                    ("Iz kojeg filma je citat 'You shall not pass!'?", "The Lord of the Rings", "Harry Potter", "Dune", "The Matrix", "A", "filmski citati", QuizDifficulty.Medium),
                    ("Iz kojeg filma je citat 'Why so serious?'?", "The Dark Knight", "Joker", "Fight Club", "Inception", "A", "filmski citati", QuizDifficulty.Hard),
                    ("Koje godine je izašao Titanic?", "1997", "1994", "2000", "2005", "A", "godine izdanja", QuizDifficulty.Easy),
                    ("Koje godine je izašao Pulp Fiction?", "1994", "1990", "1999", "2001", "A", "godine izdanja", QuizDifficulty.Medium),
                    ("Koje godine je izašao The Matrix?", "1999", "1997", "2003", "1995", "A", "godine izdanja", QuizDifficulty.Easy),
                    ("Koje godine je izašao Inception?", "2010", "2008", "2012", "2014", "A", "godine izdanja", QuizDifficulty.Medium),
                    ("Koje godine je izašao Dune?", "2021", "2019", "2023", "2017", "A", "godine izdanja", QuizDifficulty.Hard),
                    ("Koje godine je izašao Parasite?", "2019", "2017", "2021", "2015", "A", "godine izdanja", QuizDifficulty.Hard),
                    ("Koji film prati lopove koji ulaze u snove?", "Inception", "The Matrix", "Interstellar", "Dune", "A", "radnje filmova", QuizDifficulty.Medium),
                    ("Koji film prati život zatvorenika u Shawshanku?", "The Shawshank Redemption", "Goodfellas", "Fight Club", "Joker", "A", "radnje filmova", QuizDifficulty.Medium),
                    ("Koja serija prati kemičara koji postaje narko-bos?", "Breaking Bad", "The Crown", "Dark", "Stranger Things", "A", "radnje filmova", QuizDifficulty.Easy),
                    ("Koja serija prati djecu suočenu s natprirodnim u Hawkinsu?", "Stranger Things", "Dark", "Chernobyl", "The Crown", "A", "radnje filmova", QuizDifficulty.Easy),
                    ("Koja serija prati nestanak djece kroz vremenske petlje?", "Dark", "Stranger Things", "Chernobyl", "Game of Thrones", "A", "radnje filmova", QuizDifficulty.Hard),
                    ("Koji film prati mafijaša Henryja Hilla?", "Goodfellas", "The Godfather", "Pulp Fiction", "Django Unchained", "A", "radnje filmova", QuizDifficulty.Hard),
                    ("Koji film prati gladijatora Maximusa?", "Gladiator", "300", "Spartacus", "Troy", "A", "radnje filmova", QuizDifficulty.Medium),
                    ("Koja serija prati britansku kraljevsku porodicu?", "The Crown", "Dark", "Chernobyl", "Stranger Things", "A", "radnje filmova", QuizDifficulty.Medium),
                };

                foreach (var q in questions)
                {
                    db.QuizQuestions.Add(new QuizQuestion
                    {
                        Text = q.Text,
                        OptionA = q.A,
                        OptionB = q.B,
                        OptionC = q.C,
                        OptionD = q.D,
                        CorrectOption = q.Correct,
                        CategoryId = categoryLookup[q.Category].Id,
                        Difficulty = q.Difficulty
                    });
                }
                await db.SaveChangesAsync(ct);
            }
            return await db.QuizQuestions.ToListAsync(ct);
        }

        private static async Task SeedQuizSessionsAsync(
            ApplicationDbContext db, List<MyAppUser> users, List<QuizQuestion> questions, Random rng, CancellationToken ct)
        {
            if (!await db.QuizSessions.AnyAsync(ct))
            {
                var players = users.Where(u => u.Username != "desktop").ToList();
                for (int i = 0; i < 22; i++)
                {
                    var session = new QuizSession
                    {
                        UserId = players[rng.Next(players.Count)].Id,
                        PlayedAt = RecentDate(rng),
                        Score = 0,
                        Mode = rng.Next(2) == 0 ? QuizMode.Daily : QuizMode.Free,
                        IsCompleted = true
                    };

                    int answerCount = rng.Next(3, 7);
                    var picked = questions.OrderBy(_ => rng.Next()).Take(answerCount).ToList();
                    foreach (var question in picked)
                    {
                        bool correct = rng.Next(2) == 0;
                        var answer = new QuizSessionAnswer
                        {
                            QuizQuestionId = question.Id,
                            SelectedOption = correct ? question.CorrectOption : WrongOption(rng, question.CorrectOption),
                            IsCorrect = correct,
                            ResponseSeconds = Math.Round(rng.NextDouble() * 18 + 1, 1)
                        };
                        if (correct) session.Score++;
                        session.QuizSessionAnswers.Add(answer);
                    }

                    db.QuizSessions.Add(session);
                }
                await db.SaveChangesAsync(ct);
            }
        }

        private static async Task SeedThreadTagsAsync(
            ApplicationDbContext db, List<ThreadEntity> threads, List<Movie> movies, CancellationToken ct)
        {
            if (await db.Tags.AnyAsync(ct))
                return;

            string[] names = { "nolan", "plot twist", "must watch", "sci-fi", "tarantino", "classic", "hidden gem", "analysis" };
            var tags = names.Select(n => new Tag { Name = n }).ToList();
            db.Tags.AddRange(tags);
            await db.SaveChangesAsync(ct);

            for (int i = 0; i < threads.Count; i++)
            {
                var tracked = await db.Threads.FirstAsync(t => t.Id == threads[i].Id, ct);
                tracked.MovieId = movies[i % movies.Count].Id;

                foreach (var tag in new[] { tags[i % tags.Count], tags[(i + 3) % tags.Count] }.Distinct())
                    db.ThreadTags.Add(new ThreadTag { ThreadId = tracked.Id, TagId = tag.Id });
            }
            await db.SaveChangesAsync(ct);
        }

        private static async Task SeedGenrePreferencesAsync(
            ApplicationDbContext db, List<MyAppUser> users, Dictionary<string, Genre> genres, Random rng, CancellationToken ct)
        {
            if (await db.UserGenrePreferences.AnyAsync(ct))
                return;

            var genreList = genres.Values.ToList();
            foreach (var user in users.Where(u => u.Username != "desktop"))
            {
                foreach (var genre in genreList.OrderBy(_ => rng.Next()).Take(3))
                    db.UserGenrePreferences.Add(new UserGenrePreference { UserId = user.Id, GenreId = genre.Id });
            }
            await db.SaveChangesAsync(ct);
        }

        private static async Task SeedNotificationsAsync(
            ApplicationDbContext db, List<MyAppUser> users, Random rng, CancellationToken ct)
        {
            if (!await db.Notifications.AnyAsync(ct))
            {
                var defs = new (string Title, string Body)[]
                {
                    ("Dobrodošli u CineAbout!", "Hvala što ste se registrovali."),
                    ("Novi filmovi su stigli", "Pogledajte najnovije naslove."),
                    ("Kviz izazov", "Novi dnevni kviz je dostupan."),
                    ("Vaša lista za gledanje", "Film iz vaše liste je sada dostupan."),
                    ("Ažuriranje profila", "Vaš profil je uspješno ažuriran."),
                };

                var notifications = new List<Notification>();
                foreach (var d in defs)
                {
                    notifications.Add(new Notification
                    {
                        Title = d.Title,
                        Body = d.Body,
                        Data = new Dictionary<string, string> { ["type"] = "info" },
                        CreatedAt = RecentDate(rng)
                    });
                }
                db.Notifications.AddRange(notifications);
                await db.SaveChangesAsync(ct);

                var recipients = users.Where(u => u.Username != "desktop").ToList();
                for (int i = 0; i < notifications.Count; i++)
                {
                    var notification = notifications[i];
                    var recipient = recipients[i % recipients.Count];
                    db.UserNotifications.Add(new UserNotification
                    {
                        NotificationId = notification.Id,
                        UserId = recipient.Id,
                        SeenAt = i % 2 == 0 ? null : DateTime.UtcNow
                    });
                }
                await db.SaveChangesAsync(ct);
            }
        }

        private static DateTime RecentDate(Random rng)
        {
            var daysAgo = rng.Next(100) < 65 ? rng.Next(0, 7) : rng.Next(8, 30);
            return DateTime.UtcNow.AddDays(-daysAgo).AddHours(-rng.Next(0, 24));
        }

        private static string WrongOption(Random rng, string correct)
        {
            var options = new[] { "A", "B", "C", "D" }.Where(o => o != correct).ToArray();
            return options[rng.Next(options.Length)];
        }

        internal static string Slug(string value)
        {
            var sb = new StringBuilder(value.Length);
            foreach (var c in value.ToLowerInvariant())
                sb.Append(char.IsLetterOrDigit(c) ? c : '-');
            return sb.ToString();
        }

        private static int rngMonth(int year) => new Random(year).Next(1, 13);
        private static int rngDay(int year) => new Random(year + 1).Next(1, 29);

        private static void WritePlaceholder(string webRootPath, string relativePath, int width, int height, (byte R, byte G, byte B) color)
        {
            if (string.IsNullOrWhiteSpace(webRootPath)) return;
            var fullPath = Path.Combine(webRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(fullPath)) return;

            var dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            File.WriteAllBytes(fullPath, CreateSolidPng(width, height, color.R, color.G, color.B));
        }

        internal static byte[] CreateSolidPng(int width, int height, byte r, byte g, byte b)
        {
            using var ms = new MemoryStream();
            ms.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });

            using (var ihdr = new MemoryStream())
            {
                WriteInt32(ihdr, width);
                WriteInt32(ihdr, height);
                ihdr.WriteByte(8);
                ihdr.WriteByte(2);
                ihdr.WriteByte(0);
                ihdr.WriteByte(0);
                ihdr.WriteByte(0);
                WritePngChunk(ms, "IHDR", ihdr.ToArray());
            }

            using (var raw = new MemoryStream())
            {
                var row = new byte[width * 3];
                for (int x = 0; x < width; x++)
                {
                    row[x * 3] = r;
                    row[x * 3 + 1] = g;
                    row[x * 3 + 2] = b;
                }
                for (int y = 0; y < height; y++)
                {
                    raw.WriteByte(0);
                    raw.Write(row, 0, row.Length);
                }

                using var compressed = new MemoryStream();
                using (var zlib = new ZLibStream(compressed, CompressionLevel.Fastest, leaveOpen: true))
                {
                    raw.Position = 0;
                    raw.CopyTo(zlib);
                }
                WritePngChunk(ms, "IDAT", compressed.ToArray());
            }

            WritePngChunk(ms, "IEND", Array.Empty<byte>());

            return ms.ToArray();
        }

        private static void WritePngChunk(Stream output, string type, byte[] data)
        {
            WriteInt32(output, data.Length);

            var typeBytes = Encoding.ASCII.GetBytes(type);
            output.Write(typeBytes, 0, 4);
            output.Write(data, 0, data.Length);

            var crcInput = new byte[4 + data.Length];
            Buffer.BlockCopy(typeBytes, 0, crcInput, 0, 4);
            Buffer.BlockCopy(data, 0, crcInput, 4, data.Length);
            WriteUInt32(output, Crc32(crcInput));
        }

        private static void WriteInt32(Stream s, int value)
        {
            s.WriteByte((byte)(value >> 24));
            s.WriteByte((byte)(value >> 16));
            s.WriteByte((byte)(value >> 8));
            s.WriteByte((byte)value);
        }

        private static void WriteUInt32(Stream s, uint value)
        {
            s.WriteByte((byte)(value >> 24));
            s.WriteByte((byte)(value >> 16));
            s.WriteByte((byte)(value >> 8));
            s.WriteByte((byte)value);
        }

        private static readonly uint[] CrcTable = BuildCrcTable();

        private static uint[] BuildCrcTable()
        {
            var table = new uint[256];
            for (uint n = 0; n < 256; n++)
            {
                uint c = n;
                for (int k = 0; k < 8; k++)
                    c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
                table[n] = c;
            }
            return table;
        }

        private static uint Crc32(byte[] data)
        {
            uint c = 0xFFFFFFFFu;
            foreach (var b in data)
                c = CrcTable[(c ^ b) & 0xFF] ^ (c >> 8);
            return c ^ 0xFFFFFFFFu;
        }
    }
}
