using CineAboutApp.Models.Requests.AwardType;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;

namespace CineAboutApp.Interfaces.Controller
{
    public interface IAwardTypeService : ICRUDService<AwardTypeResponse, AwardTypeSearchObject, AwardTypeInsertOrUpdateRequest, AwardTypeInsertOrUpdateRequest>
    {
    }
}