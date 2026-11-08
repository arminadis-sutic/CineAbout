using CineAboutApp.Interfaces.General;
using CineAboutApp.Models.Events;
using Microsoft.Extensions.Logging;

namespace CineAboutApp.Services.General;

public class DomainEventPublisher(
    
    IRabbitMQHandler rabbitMQHandler,
    ILogger<DomainEventPublisher> logger) : IDomainEventPublisher
{
    public async Task PublishAsync<T>(T domainEvent) where T : BaseDomainEvent
    {
        try
        {
            if (domainEvent.EventType != "chat.message.sent")
            {
                
                
                logger.LogInformation("Event {EventId} of type {EventType} logged to EventLog", 
                    domainEvent.EventId, domainEvent.EventType);
            }
            await rabbitMQHandler.PublishAsync(domainEvent);
            
            logger.LogInformation("Event {EventId} published to RabbitMQ", domainEvent.EventId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish event {EventId} of type {EventType}", 
                domainEvent.EventId, domainEvent.EventType);
            throw;
        }
    }
}

