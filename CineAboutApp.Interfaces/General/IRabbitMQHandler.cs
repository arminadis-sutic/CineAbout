namespace CineAboutApp.Interfaces.General
{
    public interface IRabbitMQHandler
    {
        Task PublishAsync<T>(T msg);
    }
}
