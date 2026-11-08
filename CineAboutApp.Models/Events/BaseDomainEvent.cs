namespace CineAboutApp.Models.Events;

public abstract class BaseDomainEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string PayloadJson { get; set; } = string.Empty;
}
