using CineAboutApp.Models.Responses;

namespace CineAboutApp.Interfaces.Controller
{
    public interface IReminderService
    {
        Task<List<UpcomingTitleResponse>> GetUpcomingAsync(CancellationToken cancellationToken = default);
        Task<MyPagedList<ReminderResponse>> GetMineAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        Task<ReminderResponse> CreateAsync(int tmdbId, CancellationToken cancellationToken = default);
        Task DeleteAsync(int id, CancellationToken cancellationToken = default);

        Task<int> DispatchDueAsync(CancellationToken cancellationToken = default);
    }
}
