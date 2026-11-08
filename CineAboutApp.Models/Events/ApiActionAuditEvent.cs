namespace CineAboutApp.Models.Events;

public class ApiActionAuditEvent : BaseDomainEvent
{
    public int? ActorUserId { get; set; }
    public string? ActorUsername { get; set; }
    public List<string> Roles { get; set; } = new();
    public string HttpMethod { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string? IpAddress { get; set; }

    public ApiActionAuditEvent()
    {
        EventType = "audit.api_action";
    }
}
