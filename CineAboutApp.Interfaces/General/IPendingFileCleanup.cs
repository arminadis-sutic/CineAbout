namespace CineAboutApp.Interfaces.General
{
    public interface IPendingFileCleanup
    {
        void Enqueue(string? storedPath);

        Task<int> ProcessPendingAsync(CancellationToken cancellationToken = default);
    }
}
