using CineAboutApp.Models.Requests.Genre;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;

namespace CineAboutApp.Interfaces.Controller
{
    public interface IGenreService : ICRUDService<GenreResponse, GenreSearchObject, GenreInsertOrUpdateRequest, GenreInsertOrUpdateRequest>
    {
    }
}