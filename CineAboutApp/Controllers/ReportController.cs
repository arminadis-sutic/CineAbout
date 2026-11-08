using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Models;
using CineAboutApp.Models.Requests.Report;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineAboutApp.WebAPI.Controllers
{
    public class ReportController(IReportService service) : BaseController<ReportResponse, ReportSearchObject>(service)
    {
        [HttpGet]
        [Authorize(Roles = AppRoles.Admin)]
        public override Task<IActionResult> Get([FromQuery] ReportSearchObject search, CancellationToken cancellationToken = default)
            => base.Get(search, cancellationToken);

        [HttpGet("{id}")]
        [Authorize(Roles = AppRoles.Admin)]
        public override Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
            => base.GetById(id, cancellationToken);

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ReportResponse>> Create([FromBody] ReportInsertOrUpdateRequest request, CancellationToken cancellationToken = default)
            => Ok(await service.CreateAsync(request, cancellationToken));

        [HttpPut("{id}/decision")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<ActionResult<ReportResponse>> Decide([FromRoute] int id, [FromBody] ReportDecisionRequest request, CancellationToken cancellationToken = default)
            => Ok(await service.DecideAsync(id, request, cancellationToken));

        [HttpDelete("{id}")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken = default)
            => await service.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
    }
}
