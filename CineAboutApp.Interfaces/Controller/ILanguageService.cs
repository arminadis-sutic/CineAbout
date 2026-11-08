using CineAboutApp.Models.Requests.Language;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;

namespace CineAboutApp.Interfaces.Controller
{
    public interface ILanguageService : ICRUDService<LanguageResponse, LanguageSearchObject, LanguageInsertOrUpdateRequest, LanguageInsertOrUpdateRequest>
    {
    }
}