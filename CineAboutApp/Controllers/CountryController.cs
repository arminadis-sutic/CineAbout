using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Models;
using CineAboutApp.Models.Requests.Country;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineAboutApp.WebAPI.Controllers
{
    public class CountryController(ICountryService service) : BaseCRUDController<CountryResponse, CountrySearchObject, CountryInsertOrUpdateRequest, CountryInsertOrUpdateRequest>(service)
    {
        [HttpGet]
        [AllowAnonymous]
        public override Task<IActionResult> Get([FromQuery] CountrySearchObject search, CancellationToken cancellationToken = default)
            => base.Get(search, cancellationToken);

        [HttpGet("{id}")]
        [AllowAnonymous]
        public override Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
            => base.GetById(id, cancellationToken);

        [HttpPost]
        [Authorize(Roles = AppRoles.Admin)]
        public override Task<IActionResult> Create([FromBody] CountryInsertOrUpdateRequest request, CancellationToken cancellationToken = default)
        {
            return base.Create(request, cancellationToken);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = AppRoles.Admin)]
        public override Task<IActionResult> Update([FromRoute] int id, [FromBody] CountryInsertOrUpdateRequest request, CancellationToken cancellationToken = default)
        {
            return base.Update(id, request, cancellationToken);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = AppRoles.Admin)]
        public override Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken = default)
        {
            return base.Delete(id, cancellationToken);
        }
    }
}
