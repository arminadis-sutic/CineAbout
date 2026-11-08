using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;

namespace CineAboutApp.Interfaces.Controller
{
    public interface IFollowService
    {
        Task FollowAsync(int userId, CancellationToken cancellationToken = default);
        Task UnfollowAsync(int userId, CancellationToken cancellationToken = default);
        Task<MyPagedList<FollowUserResponse>> GetFollowersAsync(int userId, BaseSearchObject search, CancellationToken cancellationToken = default);
        Task<MyPagedList<FollowUserResponse>> GetFollowingAsync(int userId, BaseSearchObject search, CancellationToken cancellationToken = default);
        Task<MyPagedList<FeedItemResponse>> GetFeedAsync(BaseSearchObject search, CancellationToken cancellationToken = default);
    }
}
