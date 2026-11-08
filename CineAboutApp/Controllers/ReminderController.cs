using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Models.Requests.Reminder;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineAboutApp.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReminderController(IReminderService service) : ControllerBase
    {
        [HttpGet("upcoming")]
        public async Task<ActionResult<List<UpcomingTitleResponse>>> GetUpcoming(CancellationToken cancellationToken = default)
            => Ok(await service.GetUpcomingAsync(cancellationToken));

        [HttpGet]
        public async Task<ActionResult<MyPagedList<ReminderResponse>>> GetMine([FromQuery] BaseSearchObject search, CancellationToken cancellationToken = default)
            => Ok(await service.GetMineAsync(search.PageNumber ?? 1, search.PageSize ?? 20, cancellationToken));

        [HttpPost]
        public async Task<ActionResult<ReminderResponse>> Create([FromBody] ReminderInsertRequest request, CancellationToken cancellationToken = default)
            => Ok(await service.CreateAsync(request.TmdbId, cancellationToken));

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken = default)
        {
            await service.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
    }
}
