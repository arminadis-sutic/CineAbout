using EasyNetQ;
using CineAboutApp.Models.Events;
using CineAboutApp.Services.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CineAboutApp.Services.General
{
    public class SignalRBridgeHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SignalRBridgeHostedService> _logger;
        private readonly IBus _bus;

        public SignalRBridgeHostedService(
            IServiceProvider serviceProvider,
            ILogger<SignalRBridgeHostedService> logger,
            IBus bus)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _bus = bus;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SignalR Bridge Hosted Service is starting...");

            try
            {
                await _bus.PubSub.SubscribeAsync<SignalRNotificationEvent>("webapi_signalr_bridge", async data =>
                {
                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<NotificationHub>>();

                        _logger.LogInformation("Received SignalR broadcast request: Method={Method}, TargetUsers={UserCount}", 
                            data.MethodName, data.UserIds.Count);

                        foreach (var userId in data.UserIds)
                        {
                            var connections = NotificationHub.GetConnectionsForUser(userId.ToString());
                            if (connections.Any())
                            {
                                foreach (var connectionId in connections)
                                {
                                    await hubContext.Clients.Client(connectionId).SendAsync(data.MethodName, data.Payload, stoppingToken);
                                }
                            }
                            else
                            {
                                await hubContext.Clients.User(userId.ToString()).SendAsync(data.MethodName, data.Payload, stoppingToken);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing SignalR bridge event.");
                    }
                }, stoppingToken);

                _logger.LogInformation("SignalR Bridge subscribed to RabbitMQ successfully.");

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("SignalR Bridge is stopping.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in SignalR Bridge Hosted Service.");
            }
        }
    }
}
