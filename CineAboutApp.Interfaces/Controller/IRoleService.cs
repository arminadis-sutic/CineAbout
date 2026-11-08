using CineAboutApp.Models.Requests.Role;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;

namespace CineAboutApp.Interfaces.Controller
{
    public interface IRoleService : ICRUDService<RoleResponse, RoleSearchObject, RoleInsertOrUpdateRequest, RoleInsertOrUpdateRequest>
    {
        Task AddToRole(int userId, int roleId);
        Task RemoveFromRole(int userId, int roleId);
    }
}
