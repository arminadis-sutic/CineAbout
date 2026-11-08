using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Models;
using CineAboutApp.Models.Requests.Person;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineAboutApp.WebAPI.Controllers
{
    public class PersonController(IPersonService service)
        : BaseCRUDController<PersonResponse, PersonSearchObject, PersonInsertOrUpdateRequest, PersonInsertOrUpdateRequest>(service)
    {
        [HttpGet]
        public override Task<IActionResult> Get([FromQuery] PersonSearchObject search, CancellationToken cancellationToken = default)
            => base.Get(search, cancellationToken);

        [HttpGet("{id:int}/movies")]
        public async Task<IActionResult> GetMovies(int id, CancellationToken cancellationToken = default)
        {
            var movies = await service.GetMoviesAsync(id, cancellationToken);
            return movies == null ? NotFound() : Ok(movies);
        }

        [HttpGet("{id}")]
        public override Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
            => base.GetById(id, cancellationToken);

        [HttpPost]
        [Authorize(Roles = AppRoles.Admin)]
        public override Task<IActionResult> Create([FromBody] PersonInsertOrUpdateRequest request, CancellationToken cancellationToken = default)
            => base.Create(request, cancellationToken);

        [HttpPut("{id}")]
        [Authorize(Roles = AppRoles.Admin)]
        public override Task<IActionResult> Update([FromRoute] int id, [FromBody] PersonInsertOrUpdateRequest request, CancellationToken cancellationToken = default)
            => base.Update(id, request, cancellationToken);

        [HttpDelete("{id}")]
        [Authorize(Roles = AppRoles.Admin)]
        public override Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken = default)
            => base.Delete(id, cancellationToken);
    }
}
