using CineAboutApp.Models.Requests.Notification;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;

namespace CineAboutApp.Interfaces.Controller
{
    public interface INotificationService : ICRUDService<NotificationResponse, NotificationSearchObject, NotificationInsertRequest, NotificationInsertRequest>
    {
        public Task MarkAsSeen(int notificationId, CancellationToken cancellationToken);
        public Task MarkAllAsSeen(CancellationToken cancellationToken);
        public Task<int> GetUnreadCount(int userId, CancellationToken cancellationToken);
        public Task DeleteUserNotification(int notificationId, CancellationToken cancellationToken);

        public Task DeleteAllUserNotifications(CancellationToken cancellationToken);
    }
}
