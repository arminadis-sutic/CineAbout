using CineAboutApp.Models.Requests.Studio;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;

namespace CineAboutApp.Interfaces.Controller
{
    public interface IStudioService : ICRUDService<StudioResponse, StudioSearchObject, StudioInsertOrUpdateRequest, StudioInsertOrUpdateRequest>
    {
        Task<List<MovieSummaryResponse>?> GetMoviesAsync(int id, CancellationToken cancellationToken = default);
    }
}
