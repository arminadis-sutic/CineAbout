using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Models.Requests;
using CineAboutApp.Models.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineAboutApp.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserListController(IUserListService service) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<List<UserListSummaryResponse>>> Get([FromQuery] int? userId, CancellationToken ct = default)
            => Ok(await service.GetListsAsync(userId, ct));

        [HttpGet("{id:int}")]
        public async Task<ActionResult<UserListDetailResponse>> GetById(int id, CancellationToken ct = default)
        {
            var list = await service.GetAsync(id, ct);
            return list == null ? NotFound() : Ok(list);
        }

        [HttpPost]
        public async Task<ActionResult<UserListSummaryResponse>> Create([FromBody] UserListRequest request, CancellationToken ct = default)
            => Ok(await service.CreateAsync(request, ct));

        [HttpPut("{id:int}")]
        public async Task<ActionResult<UserListSummaryResponse>> Rename(int id, [FromBody] UserListRequest request, CancellationToken ct = default)
            => Ok(await service.RenameAsync(id, request, ct));

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
            => await service.DeleteAsync(id, ct) ? NoContent() : NotFound();

        [HttpPost("{id:int}/items/{movieId:int}")]
        public async Task<ActionResult<UserListDetailResponse>> AddItem(int id, int movieId, CancellationToken ct = default)
            => Ok(await service.AddItemAsync(id, movieId, ct));

        [HttpDelete("{id:int}/items/{movieId:int}")]
        public async Task<ActionResult<UserListDetailResponse>> RemoveItem(int id, int movieId, CancellationToken ct = default)
            => Ok(await service.RemoveItemAsync(id, movieId, ct));
    }
}
