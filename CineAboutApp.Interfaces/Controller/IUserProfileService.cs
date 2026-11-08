using CineAboutApp.Models.Requests.User;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.Responses.User;

namespace CineAboutApp.Interfaces.Controller
{
    public interface IUserProfileService
    {
        Task<UserProfileResponse?> GetMyProfileAsync(CancellationToken ct = default);

        Task<UserProfileResponse> UpdateProfileAsync(UserProfileUpdateRequest request, CancellationToken ct = default);

        Task<(string Path, string ContentType)> GetImageAsync(int userId, string fileName, CancellationToken ct = default);

        Task<UserPublicProfileResponse?> GetPublicProfileAsync(int userId, CancellationToken ct = default);

        Task<UserPublicProfileResponse?> GetPublicProfileAsync(string username, CancellationToken ct = default);

        Task<MyPagedList<UserSearchResponse>> SearchUsersAsync(UserSearchRequest request, CancellationToken ct = default);
    }
}
