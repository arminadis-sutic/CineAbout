using CineAboutApp.Models.Enums;
using CineAboutApp.Models.Requests.Review;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;

namespace CineAboutApp.Interfaces.Controller
{
    public interface IReviewService : ICRUDService<ReviewResponse, ReviewSearchObject, ReviewInsertOrUpdateRequest, ReviewInsertOrUpdateRequest>
    {
        Task<ReviewResponse?> UpdateStatusAsync(int id, ReviewStatus status, CancellationToken cancellationToken = default);
        Task<ReviewResponse> VoteAsync(int reviewId, bool isHelpful, CancellationToken cancellationToken = default);
        Task<ReviewResponse> RemoveVoteAsync(int reviewId, CancellationToken cancellationToken = default);
        Task<RatingSummaryResponse> GetSummaryAsync(int movieId, CancellationToken cancellationToken = default);
    }
}
