using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Models.Requests.Thread;
using CineAboutApp.Models.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineAboutApp.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CommentController(ICommentService service) : ControllerBase
    {
        [HttpPost]
        public async Task<ActionResult<CommentResponse>> Create([FromBody] CommentInsertRequest request, CancellationToken cancellationToken = default)
            => Ok(await service.CreateAsync(request, cancellationToken));

        [HttpPut("{id}")]
        public async Task<ActionResult<CommentResponse>> Update([FromRoute] int id, [FromBody] CommentUpdateRequest request, CancellationToken cancellationToken = default)
            => Ok(await service.UpdateAsync(id, request, cancellationToken));

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken = default)
        {
            await service.DeleteAsync(id, cancellationToken);
            return NoContent();
        }

        [HttpPost("{id}/vote")]
        public async Task<ActionResult<CommentResponse>> Vote([FromRoute] int id, [FromBody] VoteRequest request, CancellationToken cancellationToken = default)
            => Ok(await service.VoteAsync(id, request.IsUpvote, cancellationToken));

        [HttpDelete("{id}/vote")]
        public async Task<ActionResult<CommentResponse>> RemoveVote([FromRoute] int id, CancellationToken cancellationToken = default)
            => Ok(await service.RemoveVoteAsync(id, cancellationToken));
    }
}
