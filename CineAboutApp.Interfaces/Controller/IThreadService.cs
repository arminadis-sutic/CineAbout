using CineAboutApp.Models.Enums;
using CineAboutApp.Models.Requests.Thread;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;

namespace CineAboutApp.Interfaces.Controller
{
    public interface IThreadService : ICRUDService<ThreadResponse, ThreadSearchObject, ThreadInsertOrUpdateRequest, ThreadInsertOrUpdateRequest>
    {
        Task<ThreadResponse?> UpdateStatusAsync(int id, ThreadStatus status, CancellationToken cancellationToken = default);
        Task<ThreadResponse> VoteAsync(int threadId, bool isUpvote, CancellationToken cancellationToken = default);
        Task<ThreadResponse> RemoveVoteAsync(int threadId, CancellationToken cancellationToken = default);
        Task<List<string>> GetPopularTagsAsync(int count, CancellationToken cancellationToken = default);
    }
}
