using CineAboutApp.Models.Requests;
using CineAboutApp.Models.Responses;

namespace CineAboutApp.Interfaces.Controller
{
    public interface IUserListService
    {
        Task<List<UserListSummaryResponse>> GetListsAsync(int? userId, CancellationToken cancellationToken = default);
        Task<UserListDetailResponse?> GetAsync(int id, CancellationToken cancellationToken = default);
        Task<UserListSummaryResponse> CreateAsync(UserListRequest request, CancellationToken cancellationToken = default);
        Task<UserListSummaryResponse?> RenameAsync(int id, UserListRequest request, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
        Task<UserListDetailResponse> AddItemAsync(int id, int movieId, CancellationToken cancellationToken = default);
        Task<UserListDetailResponse> RemoveItemAsync(int id, int movieId, CancellationToken cancellationToken = default);
    }
}
