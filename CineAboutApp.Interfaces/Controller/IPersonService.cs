using CineAboutApp.Models.Requests.Person;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;

namespace CineAboutApp.Interfaces.Controller
{
    public interface IPersonService : ICRUDService<PersonResponse, PersonSearchObject, PersonInsertOrUpdateRequest, PersonInsertOrUpdateRequest>
    {
        Task<List<MovieSummaryResponse>?> GetMoviesAsync(int id, CancellationToken cancellationToken = default);
    }
}
