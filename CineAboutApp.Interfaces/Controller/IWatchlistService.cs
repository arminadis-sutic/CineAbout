using CineAboutApp.Models.Enums;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;

namespace CineAboutApp.Interfaces.Controller
{
    public interface IWatchlistService
    {
        Task<MyPagedList<WatchlistItemResponse>> GetForUserAsync(int userId, WatchlistSearchObject search, CancellationToken cancellationToken = default);
        Task<WatchlistItemResponse> SetAsync(int movieId, WatchlistType listType, CancellationToken cancellationToken = default);
        Task RemoveAsync(int movieId, CancellationToken cancellationToken = default);
    }
}
