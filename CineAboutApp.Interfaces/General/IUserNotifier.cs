namespace CineAboutApp.Interfaces.General
{
    public interface IUserNotifier
    {
        Task NotifyAsync(
            IEnumerable<int> userIds,
            string title,
            string body,
            Dictionary<string, string>? data = null,
            CancellationToken cancellationToken = default);
    }
}
