using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Model;
using CineAboutApp.Models.Requests.Language;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;
using CineAboutApp.Services.Database;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace CineAboutApp.Services.Controller
{
    public class LanguageService(ApplicationDbContext db, IMapper mapper)
        : BaseCRUDService<LanguageResponse, LanguageSearchObject, LanguageInsertOrUpdateRequest, LanguageInsertOrUpdateRequest, Language>(db, mapper), ILanguageService
    {
        protected override IQueryable<Language> ApplyFilters(IQueryable<Language> query, LanguageSearchObject search)
        {
            if (!string.IsNullOrWhiteSpace(search.NameFTS))
            {
                var term = search.NameFTS.Trim().ToLower();
                query = query.Where(x => x.Name.ToLower().Contains(term));
            }

            return base.ApplyFilters(query, search);
        }


        protected override async Task BeforeCreate(Language entity, LanguageInsertOrUpdateRequest request, CancellationToken cancellationToken = default)
        {
            request.Name = ReferenceNameGuard.Normalize(request.Name, "Jezik");
            await EnsureUniqueAsync(null, request.Name, cancellationToken);
            entity.Name = request.Name;
        }

        protected override async Task BeforeUpdate(Language entity, LanguageInsertOrUpdateRequest request, CancellationToken cancellationToken = default)
        {
            request.Name = ReferenceNameGuard.Normalize(request.Name, "Jezik");
            await EnsureUniqueAsync(entity.Id, request.Name, cancellationToken);
        }

        private async Task EnsureUniqueAsync(int? ignoreId, string name, CancellationToken cancellationToken)
        {
            if (await db.Languages.AnyAsync(l => l.Name == name && l.Id != ignoreId, cancellationToken))
            {
                throw new UserException($"Jezik '{name}' već postoji.");
            }
        }

        protected override async Task BeforeDelete(Language entity, CancellationToken cancellationToken = default)
        {
            var used = await db.Movies.CountAsync(m => m.LanguageId == entity.Id, cancellationToken);
            if (used > 0)
            {
                throw new UserException($"Jezik '{entity.Name}' je vezan za {used} filmova/serija i ne može biti obrisan.");
            }
        }
    }
}
