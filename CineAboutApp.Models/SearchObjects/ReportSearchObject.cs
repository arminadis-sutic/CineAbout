using CineAboutApp.Models.Enums;

namespace CineAboutApp.Models.SearchObjects
{
    public class ReportSearchObject : BaseSearchObject
    {
        public ReportTargetType? TargetType { get; set; }
        public ReportStatus? Status { get; set; }
        public int? TargetId { get; set; }
    }
}
