using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Models.Requests.User;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.Responses.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineAboutApp.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserProfileController(IUserProfileService service, IUserPreferenceService preferences) : ControllerBase
    {
        [HttpGet("me/genres")]
        public async Task<ActionResult<GenrePreferencesResponse>> GetMyGenres(CancellationToken ct = default)
            => Ok(await preferences.GetGenresAsync(ct));

        [HttpPut("me/genres")]
        public async Task<ActionResult<GenrePreferencesResponse>> SetMyGenres([FromBody] GenrePreferencesRequest request, CancellationToken ct = default)
            => Ok(await preferences.SetGenresAsync(request, ct));

        [HttpGet("me")]
        public async Task<ActionResult<UserProfileResponse>> GetMyProfile(CancellationToken ct = default)
        {
            var result = await service.GetMyProfileAsync(ct);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPut("me")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)]
        public async Task<ActionResult<UserProfileResponse>> UpdateMyProfile([FromForm] UserProfileUpdateRequest request, CancellationToken ct = default)
        {
            var result = await service.UpdateProfileAsync(request, ct);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<UserPublicProfileResponse>> GetPublicProfile(int id, CancellationToken ct = default)
        {
            var result = await service.GetPublicProfileAsync(id, ct);
            if (result == null) return NotFound("Profile not found or is private.");
            return Ok(result);
        }

        [HttpGet("{id:int}/image/{fileName}")]
        public async Task<IActionResult> GetImage(int id, string fileName, CancellationToken ct = default)
        {
            var (path, contentType) = await service.GetImageAsync(id, fileName, ct);
            Response.Headers.CacheControl = "private, max-age=86400";
            return PhysicalFile(path, contentType);
        }

        [HttpGet("by-username/{username}")]
        public async Task<ActionResult<UserPublicProfileResponse>> GetPublicProfileByUsername(string username, CancellationToken ct = default)
        {
            var result = await service.GetPublicProfileAsync(username, ct);
            if (result == null) return NotFound("Profile not found or is private.");
            return Ok(result);
        }

        [HttpGet("search")]
        public async Task<ActionResult<MyPagedList<UserSearchResponse>>> Search([FromQuery] UserSearchRequest request, CancellationToken ct = default)
        {
            var result = await service.SearchUsersAsync(request, ct);
            return Ok(result);
        }
    }
}
