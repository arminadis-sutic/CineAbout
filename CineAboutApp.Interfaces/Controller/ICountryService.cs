using CineAboutApp.Models.Requests.Country;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;

namespace CineAboutApp.Interfaces.Controller
{
    public interface ICountryService : ICRUDService<CountryResponse, CountrySearchObject, CountryInsertOrUpdateRequest, CountryInsertOrUpdateRequest>
    {
    }
}
