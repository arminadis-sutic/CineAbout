using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineAboutApp.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = AppRoles.Admin)]
    public class ExportController(IExportService exportService) : ControllerBase
    {
        [HttpGet("community-activity")]
        public async Task<IActionResult> ExportCommunityActivityReport(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            CancellationToken ct = default)
        {
            var bytes = await exportService.GenerateCommunityActivityReportPdfAsync(startDate, endDate, ct);
            if (bytes.Length == 0) return NotFound();
            return File(bytes, "application/pdf", $"izvjestaj-aktivnosti-zajednice_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}.pdf");
        }

        [HttpGet("content-popularity")]
        public async Task<IActionResult> ExportContentPopularityReport(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            CancellationToken ct = default)
        {
            var bytes = await exportService.GenerateContentPopularityReportPdfAsync(startDate, endDate, ct);
            if (bytes.Length == 0) return NotFound();
            return File(bytes, "application/pdf", $"izvjestaj-popularnosti-sadrzaja_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}.pdf");
        }
    }
}
