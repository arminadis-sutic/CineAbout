using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineAboutApp.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FollowController(IFollowService service) : ControllerBase
    {
        [HttpPost("{userId}")]
        public async Task<IActionResult> Follow([FromRoute] int userId, CancellationToken cancellationToken = default)
        {
            await service.FollowAsync(userId, cancellationToken);
            return NoContent();
        }

        [HttpDelete("{userId}")]
        public async Task<IActionResult> Unfollow([FromRoute] int userId, CancellationToken cancellationToken = default)
        {
            await service.UnfollowAsync(userId, cancellationToken);
            return NoContent();
        }

        [HttpGet("{userId}/followers")]
        public async Task<ActionResult<MyPagedList<FollowUserResponse>>> GetFollowers([FromRoute] int userId, [FromQuery] BaseSearchObject search, CancellationToken cancellationToken = default)
            => Ok(await service.GetFollowersAsync(userId, search, cancellationToken));

        [HttpGet("{userId}/following")]
        public async Task<ActionResult<MyPagedList<FollowUserResponse>>> GetFollowing([FromRoute] int userId, [FromQuery] BaseSearchObject search, CancellationToken cancellationToken = default)
            => Ok(await service.GetFollowingAsync(userId, search, cancellationToken));

        [HttpGet("feed")]
        public async Task<ActionResult<MyPagedList<FeedItemResponse>>> GetFeed([FromQuery] BaseSearchObject search, CancellationToken cancellationToken = default)
            => Ok(await service.GetFeedAsync(search, cancellationToken));
    }
}
