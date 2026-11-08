using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Models.Requests.Quiz;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineAboutApp.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class QuizController(IQuizService service) : ControllerBase
    {
        [HttpPost("daily/start")]
        public async Task<ActionResult<QuizStartResponse>> StartDaily(CancellationToken cancellationToken = default)
            => Ok(await service.StartDailyAsync(cancellationToken));

        [HttpPost("free/start")]
        public async Task<ActionResult<QuizStartResponse>> StartFree([FromBody] FreeQuizRequest request, CancellationToken cancellationToken = default)
            => Ok(await service.StartFreeAsync(request, cancellationToken));

        [HttpPost("submit")]
        public async Task<ActionResult<QuizResultResponse>> Submit([FromBody] QuizSubmitRequest request, CancellationToken cancellationToken = default)
            => Ok(await service.SubmitAsync(request, cancellationToken));

        [HttpGet("streak")]
        public async Task<ActionResult<StreakResponse>> GetMyStreak(CancellationToken cancellationToken = default)
            => Ok(await service.GetStreakAsync(null, cancellationToken));

        [HttpGet("streak/{userId}")]
        public async Task<ActionResult<StreakResponse>> GetStreak([FromRoute] int userId, CancellationToken cancellationToken = default)
            => Ok(await service.GetStreakAsync(userId, cancellationToken));

        [HttpGet("leaderboard")]
        public async Task<ActionResult<LeaderboardResponse>> GetLeaderboard(
            [FromQuery] LeaderboardPeriod period = LeaderboardPeriod.AllTime,
            [FromQuery] int count = 50,
            CancellationToken cancellationToken = default)
            => Ok(await service.GetLeaderboardAsync(period, count, cancellationToken));

        [HttpGet("categories")]
        public async Task<ActionResult<List<string>>> GetCategories(CancellationToken cancellationToken = default)
            => Ok(await service.GetCategoriesAsync(cancellationToken));
    }
}
