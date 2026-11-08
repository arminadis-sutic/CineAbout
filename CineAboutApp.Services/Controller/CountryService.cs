using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Models.Requests.Country;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;
using CineAboutApp.Services.Database;
using MapsterMapper;
using CineAboutApp.Model;
using Microsoft.EntityFrameworkCore;

namespace CineAboutApp.Services.Controller
{
    public class CountryService(ApplicationDbContext db, IMapper mapper) : BaseCRUDService<CountryResponse, CountrySearchObject, CountryInsertOrUpdateRequest, CountryInsertOrUpdateRequest, Country>(db, mapper), ICountryService
    {
        protected override async Task BeforeDelete(Country entity, CancellationToken cancellationToken = default)
        {
            var users = await db.MyAppUsers.CountAsync(u => u.CountryId == entity.Id, cancellationToken);
            var studios = await db.Studios.CountAsync(s => s.CountryId == entity.Id, cancellationToken);
            var cities = await db.Cities.CountAsync(c => c.CountryId == entity.Id, cancellationToken);
            if (users + studios + cities > 0)
            {
                throw new UserException($"Država '{entity.Name}' je vezana za {users} korisnika, {studios} studija i {cities} gradova i ne može biti obrisana.");
            }
        }

        protected override IQueryable<Country> ApplyFilters(IQueryable<Country> query, CountrySearchObject search)
        {
            if (!string.IsNullOrWhiteSpace(search.NameFTS))
            {
                var term = search.NameFTS.Trim().ToLower();
                query = query.Where(x => x.Name.ToLower().Contains(term));
            }

            return base.ApplyFilters(query, search);
        }

        protected override async Task BeforeCreate(Country entity, CountryInsertOrUpdateRequest request, CancellationToken cancellationToken = default)
        {
            request.Name = ReferenceNameGuard.Normalize(request.Name, "Država");
            await EnsureUniqueAsync(null, request.Name, cancellationToken);
            entity.Name = request.Name;
        }

        protected override async Task BeforeUpdate(Country entity, CountryInsertOrUpdateRequest request, CancellationToken cancellationToken = default)
        {
            request.Name = ReferenceNameGuard.Normalize(request.Name, "Država");
            await EnsureUniqueAsync(entity.Id, request.Name, cancellationToken);
        }

        private async Task EnsureUniqueAsync(int? ignoreId, string name, CancellationToken cancellationToken)
        {
            if (await db.Countries.AnyAsync(c => c.Name == name && c.Id != ignoreId, cancellationToken))
            {
                throw new UserException($"Država '{name}' već postoji.");
            }
        }
    }
}
