using CineAboutApp.Models.Requests.QuizCategory;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;

namespace CineAboutApp.Interfaces.Controller
{
    public interface IQuizCategoryService : ICRUDService<QuizCategoryResponse, QuizCategorySearchObject, QuizCategoryInsertOrUpdateRequest, QuizCategoryInsertOrUpdateRequest>
    {
    }
}
