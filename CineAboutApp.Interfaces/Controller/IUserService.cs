using CineAboutApp.Models.Requests;
using CineAboutApp.Models.Requests.User;
using CineAboutApp.Models.Responses.User;
using CineAboutApp.Models.SearchObjects;

namespace CineAboutApp.Interfaces.Controller
{
    public interface IUserService : ICRUDService<UserResponse, UserSearchObject, UserInsertOrUpdateRequest, UserInsertOrUpdateRequest>
    {
        Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
        Task<bool> LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default);
        Task<LoginResponse> GetRefreshedJwt(RefreshRequest request, CancellationToken cancellationToken = default);
        Task<string> RequestPasswordReset(string request, CancellationToken cancellationToken = default);
        Task<string> ResetPassword(PasswordResetRequest request, CancellationToken cancellationToken = default);
        Task<bool> ActivateDeactivateUserAsync(int userId, CancellationToken cancellationToken = default);
        Task<UserResponse?> GetMyProfileAsync(CancellationToken cancellationToken = default);
        Task<UserResponse> RegisterUser(UserRegisterRequest request, CancellationToken cancellationToken = default);
        Task<UserResponse> UpdateUser(int id, UserInsertOrUpdateRequest request, CancellationToken cancellationToken = default);
        Task RegisterDeviceToken(RegisterDeviceTokenRequest request, CancellationToken cancellationToken = default);

    }
}
