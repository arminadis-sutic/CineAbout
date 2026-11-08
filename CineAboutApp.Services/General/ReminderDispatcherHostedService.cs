using CineAboutApp.Interfaces.Controller;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CineAboutApp.Services.General
{
    public class ReminderDispatcherHostedService(IServiceScopeFactory scopeFactory, ILogger<ReminderDispatcherHostedService> logger)
        : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromMinutes(10);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var reminders = scope.ServiceProvider.GetRequiredService<IReminderService>();
                    var sent = await reminders.DispatchDueAsync(stoppingToken);
                    if (sent > 0)
                        logger.LogInformation("Sent {Count} release reminders.", sent);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Reminder dispatch failed; it will be retried.");
                }

                await Task.Delay(Interval, stoppingToken).ContinueWith(_ => { }, CancellationToken.None);
            }
        }
    }
}
