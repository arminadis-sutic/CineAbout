using EasyNetQ;
using CineAboutApp.Interfaces.General;
using Microsoft.Extensions.Logging;

namespace CineAboutApp.Services.General
{
    public class RabbitMQHandler(IBus bus, ILogger<RabbitMQHandler> logger) : IRabbitMQHandler
    {
        public async Task PublishAsync<T>(T msg)
        {
            try
            {
                await bus.PubSub.PublishAsync(msg);
            }
            catch (TaskCanceledException ex)
            {
                logger.LogWarning(ex, "RabbitMQ publish canceled.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "RabbitMQ publish failed.");
            }
        }
    }
}
