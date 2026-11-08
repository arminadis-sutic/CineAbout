using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Model;
using CineAboutApp.Models.Requests.QuizQuestion;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;
using CineAboutApp.Services.Database;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace CineAboutApp.Services.Controller
{
    public class QuizQuestionService(ApplicationDbContext db, IMapper mapper)
        : BaseCRUDService<QuizQuestionResponse, QuizQuestionSearchObject, QuizQuestionInsertOrUpdateRequest, QuizQuestionInsertOrUpdateRequest, QuizQuestion>(db, mapper), IQuizQuestionService
    {
        protected override IQueryable<QuizQuestion> GetIncludeQuery(IQueryable<QuizQuestion> query) => query.Include(q => q.Category);

        protected override QuizQuestionResponse MapToResponse(QuizQuestion entity)
        {
            var response = mapper.Map<QuizQuestionResponse>(entity);
            response.Category = entity.Category?.Name ?? string.Empty;
            response.CategoryId = entity.CategoryId;
            return response;
        }

        protected override async Task BeforeCreate(QuizQuestion entity, QuizQuestionInsertOrUpdateRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureValidAsync(request, cancellationToken);
            entity.CorrectOption = request.CorrectOption.Trim().ToUpperInvariant();
        }

        protected override async Task BeforeUpdate(QuizQuestion entity, QuizQuestionInsertOrUpdateRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureValidAsync(request, cancellationToken);
            request.CorrectOption = request.CorrectOption.Trim().ToUpperInvariant();
        }

        public override async Task<QuizQuestionResponse> CreateAsync(QuizQuestionInsertOrUpdateRequest request, CancellationToken cancellationToken = default)
        {
            var created = await base.CreateAsync(request, cancellationToken);
            return (await GetByIdAsync(created.Id, cancellationToken))!;
        }

        public override async Task<QuizQuestionResponse?> UpdateAsync(int id, QuizQuestionInsertOrUpdateRequest request, CancellationToken cancellationToken = default)
        {
            var updated = await base.UpdateAsync(id, request, cancellationToken);
            return updated == null ? null : await GetByIdAsync(id, cancellationToken);
        }

        private async Task EnsureValidAsync(QuizQuestionInsertOrUpdateRequest request, CancellationToken cancellationToken)
        {
            if (!await db.QuizCategories.AnyAsync(c => c.Id == request.CategoryId, cancellationToken))
            {
                throw new UserException("Odabrana kategorija ne postoji.");
            }
        }

        protected override IQueryable<QuizQuestion> ApplyFilters(IQueryable<QuizQuestion> query, QuizQuestionSearchObject search)
        {
            if (!string.IsNullOrWhiteSpace(search.NameFTS))
            {
                var term = search.NameFTS.Trim().ToLower();
                query = query.Where(x => x.Text.ToLower().Contains(term));
            }

            if (search.Difficulty.HasValue)
            {
                query = query.Where(x => x.Difficulty == search.Difficulty.Value);
            }

            if (!string.IsNullOrWhiteSpace(search.Category))
            {
                var category = search.Category.Trim().ToLower();
                query = query.Where(x => x.Category!.Name.ToLower().Contains(category));
            }

            if (search.CategoryId.HasValue)
            {
                query = query.Where(x => x.CategoryId == search.CategoryId.Value);
            }

            return base.ApplyFilters(query, search);
        }

        protected override async Task BeforeDelete(QuizQuestion entity, CancellationToken cancellationToken = default)
        {
            var used = await db.QuizSessionAnswers.CountAsync(q => q.QuizQuestionId == entity.Id, cancellationToken);
            if (used > 0)
            {
                throw new UserException($"Pitanje '{entity.Text}' je korišteno u {used} kviz odgovora i ne može biti obrisano.");
            }
        }
    }
}
