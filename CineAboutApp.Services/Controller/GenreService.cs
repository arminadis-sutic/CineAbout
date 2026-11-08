using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Model;
using CineAboutApp.Models.Requests.Genre;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;
using CineAboutApp.Services.Database;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace CineAboutApp.Services.Controller
{
    public class GenreService(ApplicationDbContext db, IMapper mapper)
        : BaseCRUDService<GenreResponse, GenreSearchObject, GenreInsertOrUpdateRequest, GenreInsertOrUpdateRequest, Genre>(db, mapper), IGenreService
    {
        protected override IQueryable<Genre> ApplyFilters(IQueryable<Genre> query, GenreSearchObject search)
        {
            if (!string.IsNullOrWhiteSpace(search.NameFTS))
            {
                var term = search.NameFTS.Trim().ToLower();
                query = query.Where(x => x.Name.ToLower().Contains(term));
            }

            return base.ApplyFilters(query, search);
        }


        protected override async Task BeforeCreate(Genre entity, GenreInsertOrUpdateRequest request, CancellationToken cancellationToken = default)
        {
            request.Name = ReferenceNameGuard.Normalize(request.Name, "Žanr");
            await EnsureUniqueAsync(null, request.Name, cancellationToken);
            entity.Name = request.Name;
        }

        protected override async Task BeforeUpdate(Genre entity, GenreInsertOrUpdateRequest request, CancellationToken cancellationToken = default)
        {
            request.Name = ReferenceNameGuard.Normalize(request.Name, "Žanr");
            await EnsureUniqueAsync(entity.Id, request.Name, cancellationToken);
        }

        private async Task EnsureUniqueAsync(int? ignoreId, string name, CancellationToken cancellationToken)
        {
            if (await db.Genres.AnyAsync(g => g.Name == name && g.Id != ignoreId, cancellationToken))
            {
                throw new UserException($"Žanr '{name}' već postoji.");
            }
        }

        protected override async Task BeforeDelete(Genre entity, CancellationToken cancellationToken = default)
        {
            var used = await db.MovieGenres.CountAsync(mg => mg.GenreId == entity.Id, cancellationToken);
            if (used > 0)
            {
                throw new UserException($"Žanr '{entity.Name}' je vezan za {used} filmova/serija i ne može biti obrisan.");
            }
        }
    }
}
