using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Model;
using CineAboutApp.Models.Requests.Studio;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;
using CineAboutApp.Services.Database;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace CineAboutApp.Services.Controller
{
    public class StudioService(ApplicationDbContext db, IMapper mapper, CineAboutApp.Interfaces.General.IIdentityService identity)
        : BaseCRUDService<StudioResponse, StudioSearchObject, StudioInsertOrUpdateRequest, StudioInsertOrUpdateRequest, Studio>(db, mapper), IStudioService
    {
        public async Task<List<MovieSummaryResponse>?> GetMoviesAsync(int id, CancellationToken cancellationToken = default)
        {
            if (!await db.Studios.AnyAsync(s => s.Id == id, cancellationToken))
            {
                return null;
            }

            var admin = identity.IsAdmin();
            return await db.Movies.AsNoTracking()
                .Where(m => m.StudioId == id && (admin || m.IsVisible))
                .OrderByDescending(m => m.ReleaseYear).ThenBy(m => m.Title)
                .Select(m => new MovieSummaryResponse
                {
                    Id = m.Id,
                    Title = m.Title,
                    ReleaseYear = m.ReleaseYear,
                    Type = m.Type,
                    PosterUrl = m.PosterUrl,
                    AverageRating = m.Reviews.Where(r => r.Status == CineAboutApp.Models.Enums.ReviewStatus.Active).Select(r => (double?)r.Rating).Average() ?? 0
                })
                .ToListAsync(cancellationToken);
        }

        protected override IQueryable<Studio> GetIncludeQuery(IQueryable<Studio> query)
        {
            return query.Include(s => s.Country);
        }

        protected override IQueryable<Studio> ApplyFilters(IQueryable<Studio> query, StudioSearchObject search)
        {
            if (!string.IsNullOrWhiteSpace(search.NameFTS))
            {
                var term = search.NameFTS.Trim().ToLower();
                query = query.Where(x => x.Name.ToLower().Contains(term));
            }

            if (search.CountryId.HasValue)
            {
                query = query.Where(x => x.CountryId == search.CountryId.Value);
            }

            return base.ApplyFilters(query, search);
        }

        protected override StudioResponse MapToResponse(Studio entity)
        {
            var response = mapper.Map<StudioResponse>(entity);
            response.CountryName = entity.Country?.Name ?? string.Empty;
            return response;
        }


        protected override async Task BeforeCreate(Studio entity, StudioInsertOrUpdateRequest request, CancellationToken cancellationToken = default)
        {
            request.Name = ReferenceNameGuard.Normalize(request.Name, "Studio");
            await EnsureUniqueAsync(null, request.Name, request.CountryId, cancellationToken);
            entity.Name = request.Name;
        }

        protected override async Task BeforeUpdate(Studio entity, StudioInsertOrUpdateRequest request, CancellationToken cancellationToken = default)
        {
            request.Name = ReferenceNameGuard.Normalize(request.Name, "Studio");
            await EnsureUniqueAsync(entity.Id, request.Name, request.CountryId, cancellationToken);
        }

        private async Task EnsureUniqueAsync(int? ignoreId, string name, int countryId, CancellationToken cancellationToken)
        {
            if (!await db.Countries.AnyAsync(c => c.Id == countryId, cancellationToken))
            {
                throw new UserException("Odabrana država ne postoji.");
            }

            if (await db.Studios.AnyAsync(s => s.Name == name && s.CountryId == countryId && s.Id != ignoreId, cancellationToken))
            {
                throw new UserException($"Studio '{name}' iz te države već postoji.");
            }
        }

        protected override async Task BeforeDelete(Studio entity, CancellationToken cancellationToken = default)
        {
            var used = await db.Movies.CountAsync(m => m.StudioId == entity.Id, cancellationToken);
            if (used > 0)
            {
                throw new UserException($"Studio '{entity.Name}' je vezan za {used} filmova/serija i ne može biti obrisan.");
            }
        }
    }
}
