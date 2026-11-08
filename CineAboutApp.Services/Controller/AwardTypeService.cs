using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Model;
using CineAboutApp.Models.Requests.AwardType;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;
using CineAboutApp.Services.Database;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace CineAboutApp.Services.Controller
{
    public class AwardTypeService(ApplicationDbContext db, IMapper mapper)
        : BaseCRUDService<AwardTypeResponse, AwardTypeSearchObject, AwardTypeInsertOrUpdateRequest, AwardTypeInsertOrUpdateRequest, AwardType>(db, mapper), IAwardTypeService
    {
        protected override IQueryable<AwardType> ApplyFilters(IQueryable<AwardType> query, AwardTypeSearchObject search)
        {
            if (!string.IsNullOrWhiteSpace(search.NameFTS))
            {
                var term = search.NameFTS.Trim().ToLower();
                query = query.Where(x => x.Name.ToLower().Contains(term));
            }

            return base.ApplyFilters(query, search);
        }


        protected override async Task BeforeCreate(AwardType entity, AwardTypeInsertOrUpdateRequest request, CancellationToken cancellationToken = default)
        {
            request.Name = ReferenceNameGuard.Normalize(request.Name, "Tip nagrade");
            await EnsureUniqueAsync(null, request.Name, cancellationToken);
            entity.Name = request.Name;
        }

        protected override async Task BeforeUpdate(AwardType entity, AwardTypeInsertOrUpdateRequest request, CancellationToken cancellationToken = default)
        {
            request.Name = ReferenceNameGuard.Normalize(request.Name, "Tip nagrade");
            await EnsureUniqueAsync(entity.Id, request.Name, cancellationToken);
        }

        private async Task EnsureUniqueAsync(int? ignoreId, string name, CancellationToken cancellationToken)
        {
            if (await db.AwardTypes.AnyAsync(a => a.Name == name && a.Id != ignoreId, cancellationToken))
            {
                throw new UserException($"Tip nagrade '{name}' već postoji.");
            }
        }

        protected override async Task BeforeDelete(AwardType entity, CancellationToken cancellationToken = default)
        {
            var used = await db.Awards.CountAsync(a => a.AwardTypeId == entity.Id, cancellationToken);
            if (used > 0)
            {
                throw new UserException($"Tip nagrade '{entity.Name}' je vezan za {used} nagrada i ne može biti obrisan.");
            }
        }
    }
}