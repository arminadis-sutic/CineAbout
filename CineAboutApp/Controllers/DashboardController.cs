using CineAboutApp.Models;
using CineAboutApp.Services.Controller;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineAboutApp.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = AppRoles.Admin)]
    public class DashboardController(DashboardService dashboardService) : ControllerBase
    {
        [HttpGet("kpis")]
        public async Task<IActionResult> GetKpis(CancellationToken ct = default)
        {
            var result = await dashboardService.GetKpisAsync(ct);
            return Ok(result);
        }

        [HttpGet("activity")]
        public async Task<IActionResult> GetActivity(
            [FromQuery] int days = 30,
            CancellationToken ct = default)
        {
            var result = await dashboardService.GetActivityAsync(days, ct);
            return Ok(result);
        }

        [HttpGet("popular-movies")]
        public async Task<IActionResult> GetPopularMovies(
            [FromQuery] int limit = 10,
            CancellationToken ct = default)
        {
            var result = await dashboardService.GetPopularMoviesAsync(limit, ct);
            return Ok(result);
        }

        [HttpGet("active-threads")]
        public async Task<IActionResult> GetActiveThreads(
            [FromQuery] int limit = 10,
            CancellationToken ct = default)
        {
            var result = await dashboardService.GetActiveThreadsAsync(limit, ct);
            return Ok(result);
        }
    }
}