using CineAboutApp.Models.Requests.Report;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;

namespace CineAboutApp.Interfaces.Controller
{
    public interface IReportService : ICRUDService<ReportResponse, ReportSearchObject, ReportInsertOrUpdateRequest, ReportInsertOrUpdateRequest>
    {
        Task<ReportResponse> DecideAsync(int id, ReportDecisionRequest request, CancellationToken cancellationToken = default);
    }
}
