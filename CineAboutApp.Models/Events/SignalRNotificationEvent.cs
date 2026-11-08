namespace CineAboutApp.Models.Events
{
    public class SignalRNotificationEvent : BaseDomainEvent
    {
        public List<int> UserIds { get; set; } = new();
        public string MethodName { get; set; } = string.Empty;
        public object? Payload { get; set; }
    }
}
