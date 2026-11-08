using CineAboutApp.Models.Requests.Thread;
using CineAboutApp.Models.Responses;

namespace CineAboutApp.Interfaces.Controller
{
    public interface ICommentService
    {
        Task<CommentResponse> CreateAsync(CommentInsertRequest request, CancellationToken cancellationToken = default);
        Task<CommentResponse> UpdateAsync(int id, CommentUpdateRequest request, CancellationToken cancellationToken = default);
        Task DeleteAsync(int id, CancellationToken cancellationToken = default);
        Task<CommentResponse> VoteAsync(int id, bool isUpvote, CancellationToken cancellationToken = default);
        Task<CommentResponse> RemoveVoteAsync(int id, CancellationToken cancellationToken = default);
    }
}
