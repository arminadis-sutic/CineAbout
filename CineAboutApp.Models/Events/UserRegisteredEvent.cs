namespace CineAboutApp.Models.Events;

public class UserRegisteredEvent : BaseDomainEvent
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime RegistrationDate { get; set; }

    public UserRegisteredEvent()
    {
        EventType = "user.registered";
    }
}
