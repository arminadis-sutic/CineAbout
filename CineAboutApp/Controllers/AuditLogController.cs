using CineAboutApp.Models;
using CineAboutApp.Models.SearchObjects;
using CineAboutApp.Services.Controller;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineAboutApp.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = AppRoles.Admin)]
    public class AuditLogController(AuditLogService service) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] AuditLogSearchObject search, CancellationToken ct = default)
        {
            var result = await service.GetAsync(search, ct);
            return Ok(result);
        }
    }
}
