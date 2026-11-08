using CineAboutApp.Models.Enums;

namespace CineAboutApp.Services.Database
{
    public class Report
    {
        public int Id { get; set; }

        public int ReporterId { get; set; }
        public MyAppUser Reporter { get; set; } = null!;

        public ReportTargetType TargetType { get; set; }
        public int TargetId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public ReportStatus Status { get; set; }
        public int? ReviewedByUserId { get; set; }
        public MyAppUser? ReviewedByUser { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? AdminNote { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
