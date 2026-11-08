using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Models;
using CineAboutApp.Models.Requests.Thread;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineAboutApp.WebAPI.Controllers
{
    public class ThreadController(IThreadService service)
        : BaseCRUDController<ThreadResponse, ThreadSearchObject, ThreadInsertOrUpdateRequest, ThreadInsertOrUpdateRequest>(service)
    {
        [HttpGet]
        public override Task<IActionResult> Get([FromQuery] ThreadSearchObject search, CancellationToken cancellationToken = default)
            => base.Get(search, cancellationToken);

        [HttpGet("{id}")]
        public override Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
            => base.GetById(id, cancellationToken);

        [HttpGet("tags")]
        public async Task<ActionResult<List<string>>> GetPopularTags([FromQuery] int count = 20, CancellationToken cancellationToken = default)
            => Ok(await service.GetPopularTagsAsync(count, cancellationToken));

        [HttpPost("{id}/vote")]
        [Authorize]
        public async Task<ActionResult<ThreadResponse>> Vote([FromRoute] int id, [FromBody] VoteRequest request, CancellationToken cancellationToken = default)
            => Ok(await service.VoteAsync(id, request.IsUpvote, cancellationToken));

        [HttpDelete("{id}/vote")]
        [Authorize]
        public async Task<ActionResult<ThreadResponse>> RemoveVote([FromRoute] int id, CancellationToken cancellationToken = default)
            => Ok(await service.RemoveVoteAsync(id, cancellationToken));

        [HttpPut("{id}/status")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> UpdateStatus([FromRoute] int id, [FromBody] ThreadStatusUpdateRequest request, CancellationToken cancellationToken = default)
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
