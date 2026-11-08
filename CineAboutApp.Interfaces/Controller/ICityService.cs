using CineAboutApp.Models.Requests.City;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;

namespace CineAboutApp.Interfaces.Controller
{
    public interface ICityService : ICRUDService<CityResponse, CitySearchObject, CityInsertOrUpdateRequest, CityInsertOrUpdateRequest>
    {
    }
}
