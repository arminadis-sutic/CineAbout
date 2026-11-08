using CineAboutApp.Models.Requests.Quiz;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;

namespace CineAboutApp.Interfaces.Controller
{
    public interface IQuizService
    {
        Task<QuizStartResponse> StartDailyAsync(CancellationToken cancellationToken = default);
        Task<QuizStartResponse> StartFreeAsync(FreeQuizRequest request, CancellationToken cancellationToken = default);
        Task<QuizResultResponse> SubmitAsync(QuizSubmitRequest request, CancellationToken cancellationToken = default);
        Task<StreakResponse> GetStreakAsync(int? userId, CancellationToken cancellationToken = default);
        Task<LeaderboardResponse> GetLeaderboardAsync(LeaderboardPeriod period, int count, CancellationToken cancellationToken = default);
        Task<List<string>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    }
}
