using CineAboutApp.Models.Requests.User;
using CineAboutApp.Models.Responses.User;

namespace CineAboutApp.Interfaces.Controller
{
    public interface IUserPreferenceService
    {
        Task<GenrePreferencesResponse> GetGenresAsync(CancellationToken cancellationToken = default);
        Task<GenrePreferencesResponse> SetGenresAsync(GenrePreferencesRequest request, CancellationToken cancellationToken = default);
    }
}
