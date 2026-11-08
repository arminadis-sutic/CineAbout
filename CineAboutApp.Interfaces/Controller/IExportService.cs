namespace CineAboutApp.Interfaces.Controller
{
    public interface IExportService
    {
        Task<byte[]> GenerateCommunityActivityReportPdfAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default);

        Task<byte[]> GenerateContentPopularityReportPdfAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default);
    }
}
