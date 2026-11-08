using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Model;
using CineAboutApp.Models.Requests.Person;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;
using CineAboutApp.Services.Database;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace CineAboutApp.Services.Controller
{
    public class PersonService(ApplicationDbContext db, IMapper mapper, CineAboutApp.Interfaces.General.IIdentityService identity)
        : BaseCRUDService<PersonResponse, PersonSearchObject, PersonInsertOrUpdateRequest, PersonInsertOrUpdateRequest, Person>(db, mapper), IPersonService
    {
        public async Task<List<MovieSummaryResponse>?> GetMoviesAsync(int id, CancellationToken cancellationToken = default)
        {
            if (!await db.Persons.AnyAsync(p => p.Id == id, cancellationToken))
            {
                return null;
            }

            var admin = identity.IsAdmin();
            return await db.MovieCasts.AsNoTracking()
                .Where(mc => mc.PersonId == id && (admin || mc.Movie.IsVisible))
                .OrderByDescending(mc => mc.Movie.ReleaseYear).ThenBy(mc => mc.Movie.Title)
                .Select(mc => new MovieSummaryResponse
                {
                    Id = mc.MovieId,
                    Title = mc.Movie.Title,
                    ReleaseYear = mc.Movie.ReleaseYear,
                    Type = mc.Movie.Type,
                    PosterUrl = mc.Movie.PosterUrl,
                    AverageRating = mc.Movie.Reviews.Where(r => r.Status == CineAboutApp.Models.Enums.ReviewStatus.Active).Select(r => (double?)r.Rating).Average() ?? 0,
                    RoleInMovie = mc.RoleInMovie
                })
                .ToListAsync(cancellationToken);
        }

        protected override IQueryable<Person> ApplyFilters(IQueryable<Person> query, PersonSearchObject search)
        {
            if (!string.IsNullOrWhiteSpace(search.NameFTS))
            {
                var term = search.NameFTS.Trim().ToLower();
                query = query.Where(x => x.FullName.ToLower().Contains(term));
            }

            if (search.RoleType.HasValue)
            {
                query = query.Where(x => x.RoleType == search.RoleType.Value);
            }

            return base.ApplyFilters(query, search);
        }

        protected override async Task BeforeDelete(Person entity, CancellationToken cancellationToken = default)
        {
            var casts = await db.MovieCasts.CountAsync(mc => mc.PersonId == entity.Id, cancellationToken);
            var awards = await db.Awards.CountAsync(a => a.PersonId == entity.Id, cancellationToken);
            if (casts + awards > 0)
            {
                throw new UserException($"Osoba '{entity.FullName}' je vezana za {casts} filmova/serija i {awards} nagrada i ne može biti obrisana.");
            }
        }
    }
}
