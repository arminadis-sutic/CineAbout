using CineAboutApp.Models.Enums;

namespace CineAboutApp.Models.Responses
{
    public class ReportResponse
    {
        public int Id { get; set; }
        public int ReporterId { get; set; }
        public string ReporterUsername { get; set; } = string.Empty;
        public ReportTargetType TargetType { get; set; }
        public int TargetId { get; set; }
        public string TargetPreview { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public ReportStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ReviewedByUsername { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? AdminNote { get; set; }
    }
}
