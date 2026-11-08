using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Models;
using CineAboutApp.Models.Requests.Role;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;
using Microsoft.AspNetCore.Authorization;

namespace CineAboutApp.WebAPI.Controllers
{
    [Authorize(Roles = AppRoles.Admin)]
    public class RoleController(IRoleService service)
        : BaseCRUDController<RoleResponse, RoleSearchObject, RoleInsertOrUpdateRequest, RoleInsertOrUpdateRequest>(service)
    {

    }
}
