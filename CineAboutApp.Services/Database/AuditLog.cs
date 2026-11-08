namespace CineAboutApp.Services.Database
{
    public class AuditLog
    {
        public int Id { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? OldValuesJson { get; set; }
        public string? NewValuesJson { get; set; }
        public int? PerformedByUserId { get; set; }
        public virtual MyAppUser? PerformedByUser { get; set; }
        public string? PerformedByUsername { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
    }
}
