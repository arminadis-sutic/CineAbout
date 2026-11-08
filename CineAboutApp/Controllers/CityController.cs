using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Models;
using CineAboutApp.Models.Requests.City;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineAboutApp.WebAPI.Controllers
{
    public class CityController(ICityService service)
        : BaseCRUDController<CityResponse, CitySearchObject, CityInsertOrUpdateRequest, CityInsertOrUpdateRequest>(service)
    {
        [HttpGet]
        public override Task<IActionResult> Get([FromQuery] CitySearchObject search, CancellationToken cancellationToken = default)
            => base.Get(search, cancellationToken);

        [HttpGet("{id}")]
        public override Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
            => base.GetById(id, cancellationToken);

        [HttpPost]
        [Authorize(Roles = AppRoles.Admin)]
        public override Task<IActionResult> Create([FromBody] CityInsertOrUpdateRequest request, CancellationToken cancellationToken = default)
            => base.Create(request, cancellationToken);

        [HttpPut("{id}")]
        [Authorize(Roles = AppRoles.Admin)]
        public override Task<IActionResult> Update([FromRoute] int id, [FromBody] CityInsertOrUpdateRequest request, CancellationToken cancellationToken = default)
            => base.Update(id, request, cancellationToken);

        [HttpDelete("{id}")]
        [Authorize(Roles = AppRoles.Admin)]
        public override Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken = default)
            => base.Delete(id, cancellationToken);
    }
}
