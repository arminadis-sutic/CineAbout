using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Models.SearchObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineAboutApp.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BaseController<T, TSearch>(IReadService<T, TSearch> service) : ControllerBase
        where TSearch : BaseSearchObject
        where T : class
    {
        [HttpGet("")]
        public virtual async Task<IActionResult> Get([FromQuery] TSearch search, CancellationToken cancellationToken = default)
        {
            var result = await service.GetAsync(search, cancellationToken);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public virtual async Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
        {
            var result = await service.GetByIdAsync(id, cancellationToken);
            if (result == null)
            {
                return NotFound();
            }
            return Ok(result);
        }
    }
}
