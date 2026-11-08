using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Interfaces.General;
using CineAboutApp.Models.Requests.Watchlist;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineAboutApp.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WatchlistController(IWatchlistService service, IIdentityService identity) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<MyPagedList<WatchlistItemResponse>>> GetMine([FromQuery] WatchlistSearchObject search, CancellationToken cancellationToken = default)
            => Ok(await service.GetForUserAsync(identity.GetCurrentUserId(), search, cancellationToken));

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<MyPagedList<WatchlistItemResponse>>> GetForUser([FromRoute] int userId, [FromQuery] WatchlistSearchObject search, CancellationToken cancellationToken = default)
            => Ok(await service.GetForUserAsync(userId, search, cancellationToken));

        [HttpPut("{movieId}")]
        public async Task<ActionResult<WatchlistItemResponse>> Set([FromRoute] int movieId, [FromBody] WatchlistSetRequest request, CancellationToken cancellationToken = default)
            => Ok(await service.SetAsync(movieId, request.ListType, cancellationToken));

        [HttpDelete("{movieId}")]
        public async Task<IActionResult> Remove([FromRoute] int movieId, CancellationToken cancellationToken = default)
        {
            await service.RemoveAsync(movieId, cancellationToken);
            return NoContent();
        }
    }
}
