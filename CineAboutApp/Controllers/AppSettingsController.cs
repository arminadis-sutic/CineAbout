using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Models;
using CineAboutApp.Models.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineAboutApp.WebAPI.Controllers
{
    [Authorize(Roles = AppRoles.Admin)]
    [Route("api/[controller]")]
    public class AppSettingsController(IAppSettingsService service) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> GetAppSettings()
        {
            return Ok(await service.GetAppSettingsAsync());
        }

        [HttpPut]
        public async Task<IActionResult> SetAppSettings([FromBody] AppSettingsResponse appSettings)
        {
            return Ok(await service.SetAppSettings(appSettings));
        }
    }
}
