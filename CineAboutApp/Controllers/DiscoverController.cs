using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Models.Requests.Discover;
using CineAboutApp.Models.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineAboutApp.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DiscoverController(IRecommendationService service) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<List<DiscoverCardResponse>>> Get([FromQuery] int count = 10, CancellationToken cancellationToken = default)
            => Ok(await service.GetDiscoverAsync(count, cancellationToken));

        [HttpPost("swipe")]
        public async Task<IActionResult> Swipe([FromBody] SwipeRequest request, CancellationToken cancellationToken = default)
        {
            await service.SwipeAsync(request.MovieId, request.Direction, cancellationToken);
            return NoContent();
        }
    }
}
