using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Interfaces.General;
using CineAboutApp.Models.Requests.Notification;
using Microsoft.Extensions.Logging;

namespace CineAboutApp.Services.General
{
    public class UserNotifier(INotificationService notifications, ILogger<UserNotifier> logger) : IUserNotifier
    {
        public async Task NotifyAsync(
            IEnumerable<int> userIds,
            string title,
            string body,
            Dictionary<string, string>? data = null,
            CancellationToken cancellationToken = default)
        {
            var recipients = userIds.Distinct().ToList();
            if (recipients.Count == 0)
                return;

            try
            {
                await notifications.CreateAsync(new NotificationInsertRequest
                {
                    Title = title,
                    Body = body,
                    Data = data ?? new Dictionary<string, string>(),
                    UserIds = recipients
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create notification '{Title}' for users {UserIds}", title, recipients);
            }
        }
    }
}
