using CineAboutApp.Models.Enums;
using CineAboutApp.Models.Responses;

namespace CineAboutApp.Interfaces.Controller
{
    public interface IRecommendationService
    {
        Task<List<DiscoverCardResponse>> GetDiscoverAsync(int count, CancellationToken cancellationToken = default);

        Task SwipeAsync(int movieId, SwipeDirection direction, CancellationToken cancellationToken = default);
    }
}
