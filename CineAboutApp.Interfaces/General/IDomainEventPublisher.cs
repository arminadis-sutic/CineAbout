using CineAboutApp.Models.Events;

namespace CineAboutApp.Interfaces.General;

public interface IDomainEventPublisher
{
    Task PublishAsync<T>(T domainEvent) where T : BaseDomainEvent;
}
