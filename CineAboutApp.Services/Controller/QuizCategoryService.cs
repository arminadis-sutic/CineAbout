using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Model;
using CineAboutApp.Models.Requests.QuizCategory;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;
using CineAboutApp.Services.Database;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace CineAboutApp.Services.Controller
{
    public class QuizCategoryService(ApplicationDbContext db, IMapper mapper)
        : BaseCRUDService<QuizCategoryResponse, QuizCategorySearchObject, QuizCategoryInsertOrUpdateRequest, QuizCategoryInsertOrUpdateRequest, QuizCategory>(db, mapper), IQuizCategoryService
    {
        protected override IQueryable<QuizCategory> ApplyFilters(IQueryable<QuizCategory> query, QuizCategorySearchObject search)
        {
            if (!string.IsNullOrWhiteSpace(search.NameFTS))
            {
                var term = search.NameFTS.Trim().ToLower();
                query = query.Where(x => x.Name.ToLower().Contains(term));
            }

            return base.ApplyFilters(query, search);
        }

        protected override async Task BeforeCreate(QuizCategory entity, QuizCategoryInsertOrUpdateRequest request, CancellationToken cancellationToken = default)
        {
            request.Name = ReferenceNameGuard.Normalize(request.Name, "Kategorija");
            await EnsureUniqueAsync(null, request.Name, cancellationToken);
            entity.Name = request.Name;
        }

        protected override async Task BeforeUpdate(QuizCategory entity, QuizCategoryInsertOrUpdateRequest request, CancellationToken cancellationToken = default)
        {
            request.Name = ReferenceNameGuard.Normalize(request.Name, "Kategorija");
            await EnsureUniqueAsync(entity.Id, request.Name, cancellationToken);
        }

        private async Task EnsureUniqueAsync(int? ignoreId, string name, CancellationToken cancellationToken)
        {
            if (await db.QuizCategories.AnyAsync(c => c.Name == name && c.Id != ignoreId, cancellationToken))
            {
                throw new UserException($"Kategorija '{name}' već postoji.");
            }
        }

        protected override async Task BeforeDelete(QuizCategory entity, CancellationToken cancellationToken = default)
        {
            var used = await db.QuizQuestions.CountAsync(q => q.CategoryId == entity.Id, cancellationToken);
            if (used > 0)
            {
                throw new UserException($"Kategorija '{entity.Name}' ima {used} pitanja i ne može biti obrisana.");
            }
        }
    }
}
