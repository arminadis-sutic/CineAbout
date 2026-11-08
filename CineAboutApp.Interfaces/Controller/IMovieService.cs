using CineAboutApp.Models.Requests.Movie;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;

namespace CineAboutApp.Interfaces.Controller
{
    public interface IMovieService : ICRUDService<MovieResponse, MovieSearchObject, MovieInsertOrUpdateRequest, MovieInsertOrUpdateRequest>
    {
    }
}
