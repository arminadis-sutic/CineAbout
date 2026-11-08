using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Interfaces.General;
using CineAboutApp.Model;
using CineAboutApp.Models.Enums;
using CineAboutApp.Services.General;
using CineAboutApp.Models.Requests.Movie;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;
using CineAboutApp.Services.Database;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace CineAboutApp.Services.Controller
{
    public class MovieService(ApplicationDbContext db, IMapper mapper, IIdentityService identity)
        : BaseCRUDService<MovieResponse, MovieSearchObject, MovieInsertOrUpdateRequest, MovieInsertOrUpdateRequest, Movie>(db, mapper), IMovieService
    {
        public override async Task<MyPagedList<MovieResponse>> GetAsync(MovieSearchObject search, CancellationToken cancellation = default)
        {
            var page = await base.GetAsync(search, cancellation);
            await EnrichAsync(page.DataItems, cancellation);
            return page;
        }

        public override async Task<MovieResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var movie = await base.GetByIdAsync(id, cancellationToken);
            if (movie == null || (!movie.IsVisible && !identity.IsAdmin()))
                return null;

            await EnrichAsync([movie], cancellationToken);
            return movie;
        }

        private async Task EnrichAsync(List<MovieResponse> movies, CancellationToken ct)
        {
            if (movies.Count == 0)
                return;

            var ids = movies.Select(m => m.Id).ToList();
            var ratings = await MovieStats.GetRatingsAsync(db, ids, ct);

            Dictionary<int, WatchlistType> myLists = [];
            var userId = identity.GetCurrentUserIdNullable();
            if (userId != null)
            {
                myLists = await db.WatchlistItems.AsNoTracking()
                    .Where(w => w.UserId == userId && ids.Contains(w.MovieId))
                    .ToDictionaryAsync(w => w.MovieId, w => w.ListType, ct);
            }

            foreach (var movie in movies)
            {
                if (ratings.TryGetValue(movie.Id, out var rating))
                {
                    movie.AverageRating = rating.Average;
                    movie.ReviewCount = rating.Count;
                }

                if (myLists.TryGetValue(movie.Id, out var listType))
                    movie.MyListType = listType;
            }
        }

        protected override IQueryable<Movie> GetIncludeQuery(IQueryable<Movie> query)
        {
            return query
                .Include(m => m.Language)
                .Include(m => m.Studio)
                .Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
                .Include(m => m.MovieCasts).ThenInclude(mc => mc.Person)
                .AsSplitQuery();
        }

        protected override IQueryable<Movie> ApplyFilters(IQueryable<Movie> query, MovieSearchObject search)
        {
            if (!string.IsNullOrWhiteSpace(search.NameFTS))
            {
                var term = search.NameFTS.Trim().ToLower();
                query = query.Where(x => x.Title.ToLower().Contains(term));
            }

            if (!identity.IsAdmin())
            {
                query = query.Where(x => x.IsVisible);
            }

            if (search.Type.HasValue)
            {
                query = query.Where(x => x.Type == search.Type.Value);
            }

            if (search.ReleaseYear.HasValue)
            {
                query = query.Where(x => x.ReleaseYear == search.ReleaseYear.Value);
            }

            if (search.GenreId.HasValue)
            {
                query = query.Where(m => m.MovieGenres.Any(mg => mg.GenreId == search.GenreId.Value));
            }

            return base.ApplyFilters(query, search);
        }

        protected override MovieResponse MapToResponse(Movie entity)
        {
            var director = entity.MovieCasts.FirstOrDefault(mc => mc.Person?.RoleType == PersonRoleType.Director)?.Person;
            return new MovieResponse
            {
                DirectorId = director?.Id,
                DirectorName = director?.FullName,
                Id = entity.Id,
                Title = entity.Title,
                OriginalTitle = entity.OriginalTitle,
                Type = entity.Type,
                ReleaseYear = entity.ReleaseYear,
                Duration = entity.Duration,
                Synopsis = entity.Synopsis,
                PosterUrl = entity.PosterUrl,
                IsVisible = entity.IsVisible,
                LanguageId = entity.LanguageId,
                LanguageName = entity.Language?.Name ?? string.Empty,
                StudioId = entity.StudioId,
                StudioName = entity.Studio?.Name ?? string.Empty,
                Genres = entity.MovieGenres
                    .Select(mg => new MovieGenreResponse { GenreId = mg.GenreId, GenreName = mg.Genre?.Name ?? string.Empty })
                    .ToList(),
                Cast = entity.MovieCasts
                    .Select(mc => new MovieCastResponse { PersonId = mc.PersonId, PersonName = mc.Person?.FullName ?? string.Empty, RoleInMovie = mc.RoleInMovie, PhotoUrl = mc.Person?.PhotoUrl })
                    .ToList()
            };
        }

        protected override Movie MapInsertToEntity(MovieInsertOrUpdateRequest request)
        {
            return new Movie
            {
                Title = request.Title,
                OriginalTitle = request.OriginalTitle,
                Type = request.Type,
                ReleaseYear = request.ReleaseYear,
                Duration = request.Duration,
                Synopsis = request.Synopsis,
                PosterUrl = request.PosterUrl,
                IsVisible = request.IsVisible,
                LanguageId = request.LanguageId,
                StudioId = request.StudioId
            };
        }

        protected override Movie MapUpdateToEntity(MovieInsertOrUpdateRequest request, Movie entity)
        {
            entity.Title = request.Title;
            entity.OriginalTitle = request.OriginalTitle;
            entity.Type = request.Type;
            entity.ReleaseYear = request.ReleaseYear;
            entity.Duration = request.Duration;
            entity.Synopsis = request.Synopsis;
            entity.PosterUrl = request.PosterUrl;
            entity.IsVisible = request.IsVisible;
            entity.LanguageId = request.LanguageId;
            entity.StudioId = request.StudioId;
            return entity;
        }

        private async Task EnsureReferencesExistAsync(MovieInsertOrUpdateRequest request, CancellationToken cancellationToken)
        {
            if (!await db.Languages.AnyAsync(l => l.Id == request.LanguageId, cancellationToken))
            {
                throw new UserException("Odabrani jezik ne postoji.");
            }

            if (!await db.Studios.AnyAsync(s => s.Id == request.StudioId, cancellationToken))
            {
                throw new UserException("Odabrani studio ne postoji.");
            }

            var genreIds = request.GenreIds.Distinct().ToList();
            if (genreIds.Count > 0 && await db.Genres.CountAsync(g => genreIds.Contains(g.Id), cancellationToken) != genreIds.Count)
            {
                throw new UserException("Neki od odabranih žanrova ne postoje.");
            }

            var personIds = request.Cast.Select(c => c.PersonId).Distinct().ToList();
            if (personIds.Count > 0 && await db.Persons.CountAsync(p => personIds.Contains(p.Id), cancellationToken) != personIds.Count)
            {
                throw new UserException("Neke od odabranih osoba ne postoje.");
            }

            if (request.Cast.GroupBy(c => c.PersonId).Any(g => g.Count() > 1))
            {
                throw new UserException("Ista osoba je navedena više puta u ekipi.");
            }
        }

        protected override async Task BeforeCreate(Movie entity, MovieInsertOrUpdateRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureReferencesExistAsync(request, cancellationToken);
            entity.MovieGenres = request.GenreIds.Distinct().Select(g => new MovieGenre { GenreId = g }).ToList();
            entity.MovieCasts = request.Cast.Select(c => new MovieCast { PersonId = c.PersonId, RoleInMovie = c.RoleInMovie }).ToList();
        }

        protected override async Task BeforeUpdate(Movie entity, MovieInsertOrUpdateRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureReferencesExistAsync(request, cancellationToken);
            await db.MovieGenres.Where(mg => mg.MovieId == entity.Id).ExecuteDeleteAsync(cancellationToken);
            await db.MovieCasts.Where(mc => mc.MovieId == entity.Id).ExecuteDeleteAsync(cancellationToken);

            foreach (var genreId in request.GenreIds.Distinct())
            {
                db.MovieGenres.Add(new MovieGenre { MovieId = entity.Id, GenreId = genreId });
            }

            foreach (var cast in request.Cast)
            {
                db.MovieCasts.Add(new MovieCast { MovieId = entity.Id, PersonId = cast.PersonId, RoleInMovie = cast.RoleInMovie });
            }
        }

        protected override async Task BeforeDelete(Movie entity, CancellationToken cancellationToken = default)
        {
            var reviews = await db.Reviews.CountAsync(r => r.MovieId == entity.Id, cancellationToken);
            var threads = await db.Threads.CountAsync(t => t.MovieId == entity.Id, cancellationToken);
            if (reviews + threads > 0)
            {
                throw new UserException($"Film '{entity.Title}' ima {reviews} recenzija i {threads} diskusija i ne može biti obrisan.");
            }

            await db.MovieGenres.Where(x => x.MovieId == entity.Id).ExecuteDeleteAsync(cancellationToken);
            await db.MovieCasts.Where(x => x.MovieId == entity.Id).ExecuteDeleteAsync(cancellationToken);
            await db.WatchlistItems.Where(x => x.MovieId == entity.Id).ExecuteDeleteAsync(cancellationToken);
            await db.SwipeHistories.Where(x => x.MovieId == entity.Id).ExecuteDeleteAsync(cancellationToken);
            await db.Awards.Where(x => x.MovieId == entity.Id).ExecuteDeleteAsync(cancellationToken);
        }

        public override async Task<MovieResponse> CreateAsync(MovieInsertOrUpdateRequest request, CancellationToken cancellationToken = default)
        {
            var created = await base.CreateAsync(request, cancellationToken);
            return (await GetByIdAsync(created.Id, cancellationToken))!;
        }

        public override async Task<MovieResponse?> UpdateAsync(int id, MovieInsertOrUpdateRequest request, CancellationToken cancellationToken = default)
        {
            var updated = await base.UpdateAsync(id, request, cancellationToken);
            return updated == null ? null : await GetByIdAsync(id, cancellationToken);
        }
    }
}
