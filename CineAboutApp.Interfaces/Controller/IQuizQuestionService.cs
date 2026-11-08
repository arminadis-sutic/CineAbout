using CineAboutApp.Models.Requests.QuizQuestion;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;

namespace CineAboutApp.Interfaces.Controller
{
    public interface IQuizQuestionService : ICRUDService<QuizQuestionResponse, QuizQuestionSearchObject, QuizQuestionInsertOrUpdateRequest, QuizQuestionInsertOrUpdateRequest>
    {
    }
}
