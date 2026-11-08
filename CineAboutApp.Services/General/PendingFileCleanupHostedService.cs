using CineAboutApp.Interfaces.General;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CineAboutApp.Services.General
{
    public class PendingFileCleanupHostedService(IServiceScopeFactory scopeFactory, ILogger<PendingFileCleanupHostedService> logger)
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
                    var cleanup = scope.ServiceProvider.GetRequiredService<IPendingFileCleanup>();
                    var handled = await cleanup.ProcessPendingAsync(stoppingToken);
                    if (handled > 0)
                    {
                        logger.LogInformation("Processed {Count} pending file deletions.", handled);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Pending file cleanup failed; it will be retried.");
                }

                await Task.Delay(Interval, stoppingToken).ContinueWith(_ => { }, CancellationToken.None);
            }
        }
    }
}
