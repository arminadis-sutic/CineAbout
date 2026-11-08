using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Model;
using CineAboutApp.Models.Requests.City;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;
using CineAboutApp.Services.Database;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace CineAboutApp.Services.Controller
{
    public class CityService(ApplicationDbContext db, IMapper mapper)
        : BaseCRUDService<CityResponse, CitySearchObject, CityInsertOrUpdateRequest, CityInsertOrUpdateRequest, City>(db, mapper), ICityService
    {
        protected override IQueryable<City> GetIncludeQuery(IQueryable<City> query) => query.Include(c => c.Country);

        protected override IQueryable<City> ApplyFilters(IQueryable<City> query, CitySearchObject search)
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

        protected override CityResponse MapToResponse(City entity)
        {
            var response = mapper.Map<CityResponse>(entity);
            response.CountryName = entity.Country?.Name ?? string.Empty;
            return response;
        }

        protected override async Task BeforeCreate(City entity, CityInsertOrUpdateRequest request, CancellationToken cancellationToken = default)
        {
            request.Name = ReferenceNameGuard.Normalize(request.Name, "Grad");
            await EnsureValidAsync(null, request, cancellationToken);
            entity.Name = request.Name;
        }

        protected override async Task BeforeUpdate(City entity, CityInsertOrUpdateRequest request, CancellationToken cancellationToken = default)
        {
            request.Name = ReferenceNameGuard.Normalize(request.Name, "Grad");
            await EnsureValidAsync(entity.Id, request, cancellationToken);
        }

        private async Task EnsureValidAsync(int? ignoreId, CityInsertOrUpdateRequest request, CancellationToken cancellationToken)
        {
            if (!await db.Countries.AnyAsync(c => c.Id == request.CountryId, cancellationToken))
            {
                throw new UserException("Odabrana država ne postoji.");
            }

            if (await db.Cities.AnyAsync(c => c.Name == request.Name && c.CountryId == request.CountryId && c.Id != ignoreId, cancellationToken))
            {
                throw new UserException($"Grad '{request.Name}' u toj državi već postoji.");
            }
        }

        protected override async Task BeforeDelete(City entity, CancellationToken cancellationToken = default)
        {
            var used = await db.UserProfiles.CountAsync(p => p.CityId == entity.Id, cancellationToken);
            if (used > 0)
            {
                throw new UserException($"Grad '{entity.Name}' je naveden u {used} profila korisnika i ne može biti obrisan.");
            }
        }
    }
}
