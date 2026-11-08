using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Models;
using CineAboutApp.Models.Requests.Review;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineAboutApp.WebAPI.Controllers
{
    public class ReviewController(IReviewService service)
        : BaseCRUDController<ReviewResponse, ReviewSearchObject, ReviewInsertOrUpdateRequest, ReviewInsertOrUpdateRequest>(service)
    {
        [HttpGet]
        public override Task<IActionResult> Get([FromQuery] ReviewSearchObject search, CancellationToken cancellationToken = default)
            => base.Get(search, cancellationToken);

        [HttpGet("{id}")]
        public override Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
            => base.GetById(id, cancellationToken);

        [HttpGet("summary/{movieId}")]
        public async Task<ActionResult<RatingSummaryResponse>> GetSummary([FromRoute] int movieId, CancellationToken cancellationToken = default)
            => Ok(await service.GetSummaryAsync(movieId, cancellationToken));

        [HttpPost("{id}/vote")]
        [Authorize]
        public async Task<ActionResult<ReviewResponse>> Vote([FromRoute] int id, [FromBody] ReviewVoteRequest request, CancellationToken cancellationToken = default)
            => Ok(await service.VoteAsync(id, request.IsHelpful, cancellationToken));

        [HttpDelete("{id}/vote")]
        [Authorize]
        public async Task<ActionResult<ReviewResponse>> RemoveVote([FromRoute] int id, CancellationToken cancellationToken = default)
            => Ok(await service.RemoveVoteAsync(id, cancellationToken));

        [HttpPut("{id}/status")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> UpdateStatus([FromRoute] int id, [FromBody] ReviewStatusUpdateRequest request, CancellationToken cancellationToken = default)
        {
            var result = await service.UpdateStatusAsync(id, request.Status, cancellationToken);
            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }
    }
}
