namespace CineAboutApp.Models.SearchObjects
{
    public class AuditLogSearchObject : BaseSearchObject
    {
        public string? EntityType { get; set; }
        public string? Action { get; set; }
        public int? PerformedByUserId { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }
}
